/*
 * Sensate IoT application class.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#include <sensateiot/application.h>
#include <sensateiot/services/messageservice.h>
#include <sensateiot/util/mongodbclientpool.h>

#include <sensateiot/httpd/httpserver.h>
#include <sensateiot/http/statushandler.h>
#include <sensateiot/http/measurementhandler.h>

#include <sensateiot/mqtt/basemqttclient.h>
#include <sensateiot/mqtt/internalmqttclient.h>

#include <sensateiot/services/userrepository.h>
#include <sensateiot/services/apikeyrepository.h>
#include <sensateiot/services/sensorrepository.h>

#include <json.hpp>

#include <unordered_set>
#include <fstream>
#include <iostream>
#include <string>

#include <google/protobuf/any.h>

template<typename T>
std::string ToHex(const T &value, size_t padding = 1)
{
	std::stringstream ss;

	ss << std::hex << value;
	return ss.str();
}

namespace sensateiot
{
	config::Config &Application::GetConfig()
	{
		return this->m_config;
	}

	void Application::SetConfig(std::string path)
	{
		this->m_configPath = std::move(path);
	}

	void Application::Run()
	{
		std::atomic_bool done = false;
		
		this->ParseConfig();
		util::Log::StartLogging(this->m_config.GetLogging());
		auto &log = util::Log::GetLog();

		log << "Starting Sensate IoT AuthService..." << util::Log::NewLine;

		util::MongoDBClientPool::Init(this->m_config.GetDatabase().GetMongoDB());

		// Internal client
		mqtt::MqttInternalCallback icb;
		auto ihost = this->m_config.GetMqtt().GetPrivateBroker().GetBroker().GetUri();
		mqtt::InternalMqttClient iclient(ihost, "3lasdfjlas", std::move(icb));
		iclient.Connect(this->m_config.GetMqtt());

		services::UserRepository users(this->m_config.GetDatabase().GetPostgreSQL());
		services::ApiKeyRepository keys(this->m_config.GetDatabase().GetPostgreSQL());
		services::SensorRepository sensors(this->m_config.GetDatabase().GetMongoDB());
		services::MessageService service(iclient, users, keys, sensors, this->m_config);

		service.LoadAll();
		log << "AuthService started!" << util::Log::NewLine;

		std::thread runner([&]() {
			while(!done) {
				auto time = service.Process();
				time_t interval = this->m_config.GetInterval();

				if(time < interval) {
					std::this_thread::sleep_for(std::chrono::milliseconds(interval - time));
				}

			}
		});

		httpd::HttpServer server(this->m_config);
		http::StatusHandler status;
		http::MeasurementHandler measurementHandler(service);

		server.AddHandler("/v1/status", status);
		server.AddHandler("/v1/processor/measurements", measurementHandler);
		server.Run();

		done = true;
		runner.join();
	}

	void Application::ParseConfig()
	{
		using namespace nlohmann;
		std::ifstream file(this->m_configPath);

		if(!file.good()) {
			throw std::runtime_error("Config file not found!");
		}

		std::string content(
				(std::istreambuf_iterator<char>(file)),
				std::istreambuf_iterator<char>());

		try {
			auto j = json::parse(content);

			this->m_config.SetInternalBatchSize(j["InternalBatchSize"]);
			this->m_config.SetBindAddress(j["BindAddress"]);
			this->m_config.SetHttpPort(j["Port"].get<std::uint16_t>());
			this->ParseMqtt(j);
			this->ParseDatabase(j);
			this->ParseLogging(j);
		} catch(json::exception &ex) {
			std::cerr << "Unable to parse configuration file: " <<
			          ex.what() << std::endl;
			throw;
		}
	}

	void Application::ParseMqtt(nlohmann::json &j)
	{
		this->m_config.SetInterval(j["Interval"]);
		this->m_config.SetWorkers(j["Workers"]);

		this->m_config.GetMqtt().GetPrivateBroker()
				.GetBroker().SetHostName(j["Mqtt"]["InternalBroker"]["Host"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.GetBroker().SetPortNumber(j["Mqtt"]["InternalBroker"]["Port"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.GetBroker().SetUsername(j["Mqtt"]["InternalBroker"]["Username"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.GetBroker().SetPassword(j["Mqtt"]["InternalBroker"]["Password"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.GetBroker().SetSsl(j["Mqtt"]["InternalBroker"]["Ssl"] == "true");
		this->m_config.GetMqtt().GetPrivateBroker()
				.SetBulkMeasurementTopic(j["Mqtt"]["InternalBroker"]["InternalBulkMeasurementTopic"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.SetMeasurementTopic(j["Mqtt"]["InternalBroker"]["InternalMeasurementTopic"]);
		this->m_config.GetMqtt().GetPrivateBroker()
				.SetMessageTopic(j["Mqtt"]["InternalBroker"]["InternalMessageTopic"]);
	}

	void Application::ParseDatabase(nlohmann::json &json)
	{
		this->m_config.GetDatabase().GetPostgreSQL()
				.SetConnectionString(json["Database"]["PgSQL"]["ConnectionString"]);
		this->m_config.GetDatabase().GetMongoDB()
				.SetDatabaseName(json["Database"]["MongoDB"]["DatabaseName"]);
		this->m_config.GetDatabase().GetMongoDB()
				.SetConnectionString(json["Database"]["MongoDB"]["ConnectionString"]);
	}

	void Application::ParseLogging(nlohmann::json &json)
	{
		this->m_config.GetLogging().SetLevel(json["Logging"]["Level"]);
		this->m_config.GetLogging().SetPath(json["Logging"]["File"]);
	}
}

void CreateApplication(const char *path)
{
	try {
		auto &app = sensateiot::Application::GetApplication();
		app.SetConfig(path);
		app.Run();
		sensateiot::util::MongoDBClientPool::Destroy();
		google::protobuf::ShutdownProtobufLibrary();
	} catch(std::runtime_error &ex) {
		std::cerr << "Unable to run application: " << ex.what();
	} catch(std::exception &ex) {
		std::cerr << "Unable to run application: " << ex.what();
	}
}
