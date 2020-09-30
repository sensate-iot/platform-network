﻿/*
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

export class WebSocketServer {
    private readonly server: http.Server;
    private readonly wss: WebSocket.Server;

    private readonly clients: WebSocketClient[];
    private readonly auditlogs: AuditLogsClient;

    public constructor(expr: express.Express,
        private readonly pool: Pool,
        private readonly timeout: number,
        private readonly secret: string
    ) {
        const server = http.createServer(expr);
        this.server = server;
        this.wss = new WebSocket.Server({ noServer: true });
        this.clients = [];
        this.auditlogs = new AuditLogsClient(pool);
    }

    public process(measurements: BulkMeasurementInfo) {
        this.clients.forEach(client => {
            if (!client.isServicing(measurements.sensorId.toString())) {
                return;
            }

            client.process(measurements);
        });
    }

    public hasOpenSocketFor(sensorId: string) {
        for (let client of this.clients) {
            if (!client.isServicing(sensorId)) {
                continue;
            }

            return true;
        }

        return false;
    }

    private destroyConnection(ws: WebSocket) {
        this.clients.forEach((info, index) => {
            if (info.compareSocket(ws)) {
                console.log("Removing web socket!");
                this.clients.splice(index, 1);
            }
        });
    }

    private async handleSocketUpgrade(socket: WebSocket, request: any) {
        let ip = getClientIP(request.headers["x-forwarded-for"]);

        if (ip === "") {
            ip = request.connection.remoteAddress.toString();
        }

        const client = new WebSocketClient(this.auditlogs,
            this.secret,
            ip,
            this.timeout,
            socket,
            this.pool);

        const hdr = request.headers["authorization"];
        console.log(`New client connection from ${ip}!`);

        if (hdr !== null && hdr !== undefined) {
            const split = hdr.split(" ");
            await client.authorize(split[1]);
        }


        this.clients.push(client);
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

            if (pathname === "/live/v1/measurements") {
                this.wss.handleUpgrade(request, socket, head, async (ws: WebSocket) => {
                    await this.handleSocketUpgrade(ws, request);
                });
            }
        });

        this.server.listen(port, () => {
            console.log(`Listening on port: ${port}!`);
        });
    }
}