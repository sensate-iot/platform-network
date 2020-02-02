﻿/*
 * Actuators controller.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SensateService.ApiCore.Attributes;
using SensateService.ApiCore.Controllers;
using SensateService.Enums;
using SensateService.Helpers;
using SensateService.Infrastructure.Repositories;
using SensateService.Models;
using SensateService.Models.Json.Out;
using SensateService.Services;
using SensateService.Services.Settings;

namespace SensateService.NetworkApi.Controllers
{
    [ApiController]
	[Produces("application/json")]
	[Route("[controller]")]
    public class ActuatorsController : AbstractDataController
    {
	    private readonly IControlMessageRepository m_controlMessages;
	    private readonly ILogger<ActuatorsController> m_logger;
	    private readonly IMqttPublishService m_publisher;
	    private readonly MqttPublishServiceOptions m_options;

        public ActuatorsController(IHttpContextAccessor ctx,
            IMqttPublishService publisher,
            ILogger<ActuatorsController> logger,
	        ISensorRepository sensors,
            IOptions<MqttPublishServiceOptions> options,
	        IControlMessageRepository msgs) : base(ctx, sensors)
        {
	        this.m_controlMessages = msgs;
	        this.m_logger = logger;
	        this.m_publisher = publisher;
	        this.m_options = options.Value;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new [] { "value1", "value2" };
        }

        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost]
        [ValidateModel]
        [ReadWriteApiKey]
        public async Task<IActionResult> Post([FromBody] ControlMessage msg)
        {
	        var auth = await this.AuthenticateUserForSensor(msg.SensorId.ToString()).AwaitBackground();

	        if(!auth) {
		        return this.Forbid();
	        }

            msg.Timestamp = DateTime.UtcNow;

	        try {
		        var asyncio = new Task[2];
		        var topic = this.m_options.ActuatorTopic.Replace("$sensorId", msg.SensorId.ToString());

		        asyncio[0] = this.m_controlMessages.CreateAsync(msg);
		        asyncio[1] = this.m_publisher.PublishOnAsync(topic, JsonConvert.SerializeObject(msg), false);
		        await Task.WhenAll(asyncio).AwaitBackground();
	        } catch(Exception ex) {
                this.m_logger.LogInformation($"Unable to send control message: {ex.Message}");
                this.m_logger.LogDebug(ex.StackTrace);

                return this.BadRequest(new Status {
	                Message = "Unable to send control message!",
	                ErrorCode = ReplyCode.BadInput
                });
	        }

	        return this.NoContent();
        }

        // PUT: api/Actuators/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
