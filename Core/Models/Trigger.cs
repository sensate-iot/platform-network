﻿/*
 * Data trigger model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace SensateService.Models
{
	public class Trigger
	{
		[Required, Key]
		public long Id { get; set; }
		[Required]
		public string KeyValue { get; set; }
		public decimal? LowerEdge { get; set; }
		public decimal? UpperEdge { get; set; }
		[Required, StringLength(24, MinimumLength = 24)]
		public string SensorId { get; set; }
		public DateTime LastTriggered { get; set; }
		public ICollection<TriggerAction> Actions { get; set; }
	}
}