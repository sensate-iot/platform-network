/*
 * Sensate SQL database context.
 *
 * @author: Michel Megens
 * @email:  dev@bietje.net
 */

using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SensateService.Models;
using Microsoft.AspNetCore.Identity;

namespace SensateService.Infrastructure.Sql
{
	public class SensateSqlContext : IdentityDbContext<SensateUser, UserRole, string>
	{
		public new DbSet<SensateUser> Users { get; set; }
		public DbSet<AuditLog> AuditLogs { get; set; }
		public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
		public DbSet<ChangeEmailToken> ChangeEmailTokens { get; set; }
		public new DbSet<UserToken> UserTokens { get; set; }

		public SensateSqlContext(DbContextOptions<SensateSqlContext> options) :
			base(options)
		{}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<PasswordResetToken>().HasKey(
				k => k.UserToken
			);

			builder.Entity<UserToken>().HasKey(k => new {
				k.UserId, k.Value
			});
		}
	}
}
