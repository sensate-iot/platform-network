﻿/*
 * Email sender service using SendGrind.com.
 * 
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using SendGrid;
using SendGrid.Helpers.Mail;

using SensateIoT.Platform.Network.Adapters.Abstract;
using SensateIoT.Platform.Network.Data.DTO;

namespace SensateIoT.Platform.Network.Adapters.Mail
{
	public class SendGridMailer : IEmailSender
	{
		private SendGridAuthOptions _options;

		public SendGridMailer(IOptions<SendGridAuthOptions> opts)
		{
			this._options = opts.Value;
		}

		public async Task SendEmailAsync(string recip, string subj, EmailBody body)
		{
			await this.Execute(recip, subj, body);
		}

		private Task<Response> Execute(string key, string recip, string subj, string body)
		{
			SendGridMessage msg;
			var client = new SendGridClient(key);

			msg = new SendGridMessage() {
				From = new EmailAddress(this._options.From, this._options.FromName),
				Subject = subj,
				PlainTextContent = body,
				HtmlContent = body,
			};

			msg.AddTo(recip);
			return client.SendEmailAsync(msg);
		}

		private Task<Response> Execute(string recip, string subj, EmailBody body)
		{
			var client = new SendGridClient(this._options.Key);


			body.AddRecip(recip);
			body.FromEmail = this._options.From;
			body.FromName = this._options.FromName;
			body.Subject = subj;

			var msg = body.BuildSendgridMessage();

			return client.SendEmailAsync(msg);

		}
	}
}
