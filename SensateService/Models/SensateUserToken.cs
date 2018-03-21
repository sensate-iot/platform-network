/*
 * User token data model. Table model for JWT tokens and JWT
 * refresh tokens.
 *
 * @author Michel Megens
 * @email  dev@bietje.net
 */

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SensateService.Models
{
	[Table("AspNetAuthTokens")]
	public class SensateUserToken
	{
		public bool Valid { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }
		public string Value { get; set; }
		public string LoginProvider { get; set; }

		[ForeignKey("User")]
		public string UserId { get; set; }
		public SensateUser User { get; set; }

		public SensateUserToken()
		{
			this.Valid = true;
			this.CreatedAt = DateTime.Now;
		}

		public SensateUserToken(TimeSpan expiresIn) : this()
		{
			this.ExpiresAt = DateTime.Now.Add(expiresIn);
		}
	}
}