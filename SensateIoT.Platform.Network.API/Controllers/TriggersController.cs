/*
 * Trigger controller implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;
using SensateIoT.Platform.Network.API.Abstract;
using SensateIoT.Platform.Network.API.Attributes;
using SensateIoT.Platform.Network.API.DTO;
using SensateIoT.Platform.Network.Data.DTO;
using SensateIoT.Platform.Network.Data.Enums;
using SensateIoT.Platform.Network.Data.Models;
using SensateIoT.Platform.Network.DataAccess.Abstract;
using Message = SensateIoT.Platform.Network.Data.Models.Message;
using TriggerAction = SensateIoT.Platform.Network.Data.Models.TriggerAction;

namespace SensateIoT.Platform.Network.API.Controllers
{
	[Produces("application/json")]
	[Route("network/v1/[controller]")]
	public class TriggersController : AbstractApiController
	{
		private readonly ITriggerAdministrationRepository m_triggers;
		private readonly ICommandPublisher m_mqtt;

		public TriggersController(IHttpContextAccessor ctx,
								  ISensorRepository sensors,
								  ISensorLinkRepository links,
								  ITriggerAdministrationRepository triggers,
								  ICommandPublisher publisher,
								  IApiKeyRepository keys) : base(ctx, sensors, links, keys)
		{
			this.m_triggers = triggers;
			this.m_mqtt = publisher;
		}

		[HttpPost]
		[ReadWriteApiKey, ValidateModel]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<Trigger>), StatusCodes.Status201Created)]
		public async Task<IActionResult> Create([FromBody] RawTrigger raw)
		{
			Trigger trigger;
			var invalidInput = new Response<string>();

			trigger = null;

			if(raw == null) {
				return this.UnprocessableEntity(invalidInput);
			}

			var type = raw.Type;

			if(string.IsNullOrEmpty(raw.SensorId) || !ObjectId.TryParse(raw.SensorId, out _)) {
				invalidInput.AddError("Invalid value in the 'sensorId' field.");
				return this.UnprocessableEntity(invalidInput);
			}

			var auth = await this.AuthenticateUserForSensor(raw.SensorId).ConfigureAwait(false);

			if(!auth) {
				return this.CreateNotAuthorizedResult();
			}

			if(type == TriggerType.EdgeTrigger) {
				trigger = CreateNumberTrigger(raw);
			} else if(type == TriggerType.Regex) {
				trigger = CreateNaturalLanguageTrigger(raw, invalidInput);
			}

			if(trigger == null) {
				return this.UnprocessableEntity(invalidInput);
			}

			trigger.Type = type;
			await this.m_triggers.CreateAsync(trigger).ConfigureAwait(false);
			await this.m_mqtt.PublishCommandAsync(CommandType.AddSensor, raw.SensorId).ConfigureAwait(false);

			return this.CreatedAtAction(nameof(this.GetById), new { triggerId = trigger.ID },
										new Response<Trigger>(trigger));
		}

		[HttpGet("{triggerId}")]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<Trigger>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetById(long triggerId)
		{
			var trigger = await this.m_triggers.GetAsync(triggerId).ConfigureAwait(false);

			if(trigger == null) {
				return this.NotFound();
			}

			var linked = await this.IsLinkedSensor(trigger.SensorID).ConfigureAwait(false);
			var auth = await this.AuthenticateUserForSensor(trigger.SensorID).ConfigureAwait(false);

			if(!auth && !linked) {
				return this.CreateNotAuthorizedResult();
			}

			return this.Ok(new Response<Trigger>(trigger));
		}

		[HttpDelete("{triggerId}")]
		[ReadWriteApiKey]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<IActionResult> Delete(long triggerId)
		{
			var trigger = await this.m_triggers.GetAsync(triggerId).ConfigureAwait(false);

			if(trigger == null) {
				return this.NotFound();
			}

			var sensor = await this.m_sensors.GetAsync(trigger.SensorID).ConfigureAwait(false);

			if(sensor == null || sensor.Owner != this.CurrentUser.ID.ToString()) {
				return this.Forbidden();
			}

			await this.m_triggers.DeleteAsync(trigger.ID).ConfigureAwait(false);
			await this.m_mqtt.PublishCommandAsync(CommandType.AddSensor, sensor.InternalId.ToString()).ConfigureAwait(false);
			return this.NoContent();
		}

		[HttpDelete("{triggerId}/actions")]
		[ReadWriteApiKey]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<IActionResult> RemoveAction(long triggerId, [FromQuery] TriggerChannel? channel)
		{
			var invalidResponse = new Response<string>();

			if(channel == null) {
				invalidResponse.AddError("Required property 'channel' is not set.");
				return this.UnprocessableEntity(invalidResponse);
			}

			var trigger = await this.m_triggers.GetAsync(triggerId).ConfigureAwait(false);

			if(trigger == null) {
				return this.NotFound();
			}

			var sensor = await this.m_sensors.GetAsync(trigger.SensorID).ConfigureAwait(false);

			if(sensor == null || sensor.Owner != this.CurrentUser.ID.ToString()) {
				return this.Forbidden();
			}

			try {
				await this.m_triggers.RemoveActionAsync(trigger, channel.Value).ConfigureAwait(false);
				await this.m_mqtt.PublishCommandAsync(CommandType.AddSensor, sensor.InternalId.ToString()).ConfigureAwait(false);
			} catch(ArgumentException) {
				return this.NotFound();
			}

			return this.NoContent();
		}

		[HttpPost("{triggerId}/actions")]
		[ReadWriteApiKey]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<Trigger>), StatusCodes.Status201Created)]
		public async Task<IActionResult> AddAction(long triggerId, [FromBody] TriggerAction action)
		{
			var invalidResponse = new Response<string>();

			if(action == null) {
				invalidResponse.AddError("No actions provided to add.");
				return this.UnprocessableEntity(invalidResponse);
			}

			if(action.Channel > TriggerChannel.MQTT && string.IsNullOrEmpty(action.Target)) {
				invalidResponse.AddError("Missing target attribute.");
				return this.UnprocessableEntity(invalidResponse);
			}

			var trigger = await this.m_triggers.GetAsync(triggerId).ConfigureAwait(false);

			action.TriggerID = triggerId;

			if(trigger == null) {
				return this.NotFound();
			}

			var authForTrigger = await this.AuthenticateUserForSensor(trigger.SensorID).ConfigureAwait(false);

			var error = new Response<string>();
			if(action.Channel > TriggerChannel.MQTT) {
				authForTrigger = await this.CreatedTargetedActionAsync(action, error).ConfigureAwait(false);
			}

			if(!authForTrigger) {
				return this.UnprocessableEntity(error);
			}

			try {
				await this.m_triggers.AddActionAsync(trigger, action).ConfigureAwait(false);
				await this.m_mqtt.PublishCommandAsync(CommandType.AddSensor, trigger.SensorID).ConfigureAwait(false);
			} catch(FormatException ex) {
				var response = new Response<string>();

				response.AddError(ex.Message);
				return this.UnprocessableEntity(response);
			}

			trigger.TriggerActions ??= new List<TriggerAction>();
			trigger.TriggerActions.Add(action);

			return this.CreatedAtAction(nameof(this.GetById), new { triggerId = trigger.ID }, new Response<Trigger>(trigger));
		}

		[HttpPost("{triggerId}/add-actions")]
		[ReadWriteApiKey]
		[ValidateModel]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(Response<Trigger>), StatusCodes.Status201Created)]
		public async Task<IActionResult> AddActions(long triggerId, [FromBody] List<TriggerAction> actions)
		{
			var invalidResponse = new Response<string>();

			if(actions == null) {
				invalidResponse.AddError("No actions provided to add.");
				return this.UnprocessableEntity(invalidResponse);
			}

			actions.ForEach(action => action.TriggerID = triggerId);
			var trigger = await this.m_triggers.GetAsync(triggerId).ConfigureAwait(false);

			if(trigger == null) {
				return this.NotFound();
			}

			var auth = await this.AuthenticateUserForSensor(trigger.SensorID).ConfigureAwait(false);

			if(!auth) {
				return this.Forbidden();
			}

			await this.m_triggers.AddActionsAsync(trigger, actions).ConfigureAwait(false);
			await this.m_mqtt.PublishCommandAsync(CommandType.AddSensor, trigger.SensorID).ConfigureAwait(false);
			return this.CreatedAtAction(nameof(this.GetById), new { triggerId = trigger.ID }, new Response<Trigger>(trigger));
		}

		private async Task<bool> CreatedTargetedActionAsync(TriggerAction action, Response<string> response)
		{
			bool auth;

			if(action.Target == null)
				return false;

			if(action.Channel == TriggerChannel.ControlMessage) {
				var actuator = await this.m_sensors.GetAsync(action.Target).ConfigureAwait(false);

				if(actuator == null) {
					return false;
				}

				auth = await this.AuthenticateUserForSensor(actuator, false).ConfigureAwait(false);

				if(!auth) {
					response.AddError("Unable to authorize target sensor/actuator!");
				}
			} else {
				auth = Uri.TryCreate(action.Target, UriKind.Absolute, out var result) &&
					result.Scheme == Uri.UriSchemeHttp || result?.Scheme == Uri.UriSchemeHttps;

				if(!auth) {
					response.AddError("Invalid URL in target field.");
				}
			}

			return auth;
		}

		private static Trigger CreateNaturalLanguageTrigger(RawTrigger raw, Response<string> errorResponse)
		{
			if(string.IsNullOrEmpty(raw.FormalLanguage)) {
				errorResponse.AddError("Missing a formal language definition!");
				return null;
			}

			var trigger = new Trigger {
				FormalLanguage = raw.FormalLanguage,
				SensorID = raw.SensorId,
				KeyValue = nameof(Message.Data)
			};

			return trigger;
		}

		private static Trigger CreateNumberTrigger(RawTrigger raw)
		{
			if((raw.LowerEdge != null || raw.UpperEdge != null) && raw.KeyValue == null) {
				return null;
			}

			if(string.IsNullOrEmpty(raw.KeyValue)) {
				return null;
			}

			var trigger = new Trigger {
				KeyValue = raw.KeyValue,
				UpperEdge = raw.UpperEdge,
				LowerEdge = raw.LowerEdge,
				SensorID = raw.SensorId,
			};

			return trigger;
		}

		private IActionResult CreateNotAuthorizedResult()
		{
			var response = new Response<string>();

			response.AddError("Unable to authorize user!");
			return this.Unauthorized(response);
		}
	}
}
