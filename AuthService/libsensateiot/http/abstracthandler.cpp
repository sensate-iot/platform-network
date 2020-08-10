/*
 * HTTP abstract handler.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#include <sensateiot/httpd/httpresponse.h>
#include <sensateiot/httpd/httprequest.h>
#include <sensateiot/http/abstracthandler.h>

namespace sensateiot::http
{
	httpd::HttpResponse AbstractHandler::HandleRequest(const httpd::HttpRequest& request)
	{
		httpd::HttpResponse response;

		response.Data().clear();
		response.ContentType().assign("application/json");
		response.Server().assign("Sensate IoT/AuthService");
		response.SetStatus(boost::beast::http::status::no_content);

		return response;
	}

	httpd::HttpResponse AbstractHandler::HandleInvalidMethod()
	{
		httpd::HttpResponse response;

		response.Data().assign(R"({"message": "No route has been defined."})");
		response.SetStatus(boost::beast::http::status::method_not_allowed);
		response.SetKeepAlive(false);

		return response;
	}

	httpd::HttpResponse AbstractHandler::HandleUnprocessable()
	{
		httpd::HttpResponse response;

		response.Data().assign(R"({"message": "Unable to process request."})");
		response.SetStatus(boost::beast::http::status::unprocessable_entity);
		response.SetKeepAlive(false);

		return response;
	}
}