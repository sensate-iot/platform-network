/*
 * SMTP email interface.
 * 
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System.Threading.Tasks;
using SensateIoT.Platform.Network.Data.DTO;

namespace SensateIoT.Platform.Network.Adapters.Abstract
{
	public interface IEmailSender
	{
		Task SendEmailAsync(string recip, string subj, EmailBody body);
	}
}
