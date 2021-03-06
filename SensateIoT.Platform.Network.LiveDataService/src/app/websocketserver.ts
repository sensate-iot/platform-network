/*
 * WebSocket handler class.
 * 
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

import * as http from "http";
import * as WebSocket from "ws";
import * as express from "express";
import * as url from "url";

import { BulkMeasurementInfo } from "../models/measurement";
import { WebSocketClient } from "../clients/websocketclient";
import { Pool } from "pg";
import { AuditLogsClient } from "../clients/auditlogsclient";
import { getClientIP } from "./util";
import { ClientType } from "../models/clienttype";
import { BulkMessageInfo } from "../models/message";
import { ApiKeyClient } from "../clients/apikeyclient";
import { MqttClient } from "../clients/mqttclient";
import { Command, stringifyCommand } from "../commands/command";
import { SyncLiveDataSensorsCommand } from "../commands/synclivedatasensorscommand";
import { Application } from "./app";
import { BulkControlMessage } from "../models/controlmessage";
import { SubscriptionService } from "./subscriptionservice";

export class WebSocketServer {
    private readonly server: http.Server;
    private readonly wss: WebSocket.Server;

    private readonly measurementClients: WebSocketClient[];
    private readonly messageClients: WebSocketClient[];
    private readonly controlClients: WebSocketClient[];
    private readonly auditlogs: AuditLogsClient;
    private readonly apikeys: ApiKeyClient;
    private readonly subcriptions: SubscriptionService;

    public constructor(expr: express.Express,
        pool: Pool,
        private readonly timeout: number,
        private readonly secret: string,
        private readonly mqtt: MqttClient
    ) {
        this.subcriptions = new SubscriptionService();
        const server = http.createServer(expr);
        this.server = server;
        this.wss = new WebSocket.Server({ noServer: true });
        this.measurementClients = [];
        this.messageClients = [];
        this.controlClients = [];
        this.auditlogs = new AuditLogsClient(pool);
        this.apikeys = new ApiKeyClient(pool);

        setInterval(() => {
            this.publicationHandler();
        }, 60000);
    }

    private publicationHandler(): void {
        console.log("Updating message routers!");

        const cmd: Command<SyncLiveDataSensorsCommand> = {
            cmd: "synclivedatasensors",
            arguments: {
                target: Application.config.id,
                sensors: this.subcriptions.getSubscribers()
            }
        }

        this.mqtt.publish(Application.config.mqtt.routerCommandTopic, stringifyCommand(cmd));
    }

    public processMessages(messages: BulkMessageInfo) {
        this.messageClients.forEach(client => {
            if (messages.sensorId === null || messages.sensorId == undefined) {
                return;
            }

            if (!client.isServicing(messages.sensorId.toString())) {
                return;
            }

            const data = JSON.stringify(messages);
            client.process(data);
        });
    }

    public processMeasurements(measurements: BulkMeasurementInfo) {
        this.measurementClients.forEach(client => {
            if (measurements.sensorId === null || measurements.sensorId == undefined) {
                return;
            }

            if (!client.isServicing(measurements.sensorId.toString())) {
                return;
            }

            const data = JSON.stringify(measurements);
            client.process(data);
        });
    }

    public processControlMessages(messages: BulkControlMessage) {
        this.controlClients.forEach(client => {
            if (messages.sensorId === null || messages.sensorId == undefined) {
                return;
            }

            if (!client.isServicing(messages.sensorId.toString())) {
                return;
            }

            const data = JSON.stringify(messages);
            client.process(data);
        });

    }

    private getClientsFor(type: ClientType) {
        switch (type) {
            case ClientType.ControlMessageClient:
                return this.controlClients;

            case ClientType.MeasurementClient:
                return this.measurementClients;

            default:
            case ClientType.MessageClient:
                return this.messageClients;
        }
    }

    public hasOpenSocketFor(sensorId: string, type: ClientType) {
        const clients = this.getClientsFor(type);

        for (let client of clients) {
            if (!client.isServicing(sensorId)) {
                continue;
            }

            return true;
        }

        return false;
    }

    private destroyConnection(ws: WebSocket) {
        WebSocketServer.removeClient(this.measurementClients, ws);
        WebSocketServer.removeClient(this.messageClients, ws);
        WebSocketServer.removeClient(this.controlClients, ws);
    }

    private static removeClient(clients: WebSocketClient[], ws: WebSocket) {
        clients.every((info, index) => {
            if (info.compareSocket(ws)) {
                console.log("Removing web socket!");
                const client = clients.splice(index, 1);
                client[0].unsubscribeAll();

                return false; /* Stop iterating */
            }

            return true;
        });
    }

    private static getIpFromRequest(request: any): string {
        let ip = getClientIP(request.headers["x-forwarded-for"]);

        if (ip === "") {
            ip = request.connection.remoteAddress.toString();
        }

        return ip;
    }

    private async handleSocketUpgrade(socket: WebSocket, request: any, type: ClientType) {
        const ip = WebSocketServer.getIpFromRequest(request);

        const client = new WebSocketClient(
            this.subcriptions,
            this.auditlogs,
            this.apikeys,
            this.secret,
            ip,
            this.timeout,
            type,
            this.mqtt,
            socket);

        const hdr = request.headers["authorization"];
        console.log(`New client connection from ${ip}!`);

        if (hdr !== null && hdr !== undefined) {
            const split = hdr.split(" ");
            await client.authorize(split[1]);
        }

        switch (type) {
            case ClientType.MeasurementClient:
                this.measurementClients.push(client);
                break;

            case ClientType.MessageClient:
                this.messageClients.push(client);
                break;

            case ClientType.ControlMessageClient:
                this.controlClients.push(client);
                break;
        }

        this.wss.emit("connection", socket, request);
    }

    public listen(port: number) {
        this.wss.on("connection", (ws: WebSocket) => {
            ws.on("close", () => {
                this.destroyConnection(ws);
                ws.close();
            });
        });

        this.server.on("upgrade", async (request, socket, head) => {
            const pathname = url.parse(request.url).pathname;

            switch (pathname) {
                case "/live/v1/measurements":
                    this.wss.handleUpgrade(request, socket, head, async (ws: WebSocket) => {
                        await this.handleSocketUpgrade(ws, request, ClientType.MeasurementClient);
                    });
                    break;

                case "/live/v1/messages":
                    this.wss.handleUpgrade(request, socket, head, async (ws: WebSocket) => {
                        await this.handleSocketUpgrade(ws, request, ClientType.MessageClient);
                    });
                    break;

                case "/live/v1/control":
                    this.wss.handleUpgrade(request, socket, head, async (ws: WebSocket) => {
                        await this.handleSocketUpgrade(ws, request, ClientType.ControlMessageClient);
                    });
                    break;

                default:
                    const ip = WebSocketServer.getIpFromRequest(request);
                    console.log(`Client connecting to invalid path (${ip}).`);
                    break;
            }
        });

        this.server.listen(port, () => {
            console.log(`Listening on port: ${port}!`);
        });
    }
}
