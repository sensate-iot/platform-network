/*
 * HTTP measurement handler.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#pragma once

#include <sensateiot/httpd/httprequest.h>
#include <sensateiot/httpd/httpresponse.h>
#include <sensateiot/http/abstracthandler.h>
#include <sensateiot/services/messageservice.h>
#include <sensateiot/stl/referencewrapper.h>
#include <sensateiot/data/bulkmeasurementvalidator.h>

#include <string>

namespace sensateiot::http
{
	class BulkMeasurementHandler : public AbstractHandler {
	public:
		explicit BulkMeasurementHandler() = default;
		explicit BulkMeasurementHandler(services::MessageService& service);

		httpd::HttpResponse HandleRequest(const httpd::HttpRequest& request) override;

	private:
		stl::ReferenceWrapper<services::MessageService> m_service;
		data::BulkMeasurementValidator m_validator;

		static constexpr auto AcceptedMessage = std::string_view(R"({"message":"Measurements queued.","count":%llu})");
	};
}
