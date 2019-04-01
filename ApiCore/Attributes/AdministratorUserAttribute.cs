/*
 * Attribute to restrict access to the Administrators group.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

using Microsoft.AspNetCore.Authorization;

namespace SensateService.ApiCore.Attributes
{
	public class AdministratorUserAttribute : AuthorizeAttribute
	{
		public AdministratorUserAttribute() : base()
		{
			this.Roles = "Administrators";
		}
	}
}