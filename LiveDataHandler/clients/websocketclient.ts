/*
 * WebSocket client implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

import { Sensor, SensorModel } from "../models/sensor";
import { Types } from "mongoose";
import {createHash} from "crypto";
import * as WebSocket from "ws";
import { ISensorAuthRequest } from "../models/sensorauthrequest";
import { BulkMeasurementInfo } from "../models/measurement";
import { IWebSocketRequest } from "../models/request";
import * as jwt from "jsonwebtoken";
import { SensorLinksClient } from "./sensorlinksclient";
import { Pool } from "pg";

// ReSharper disable once UnusedLocalImport
import moment from "moment";

export class WebSocketClient {
    private readonly sensors: Map<string, SensorModel>;
    private readonly socket: WebSocket;
    private authorized: boolean;
    private readonly client: SensorLinksClient;
    private userId: string;

    private static timeout = 250;

    public constructor(socket: WebSocket, pool: Pool, private readonly secret: string) {
        this.socket = socket;
        this.sensors = new Map<string, SensorModel>();
        this.socket.onmessage = this.onMessage.bind(this);
        this.authorized = false;
        this.client = new SensorLinksClient(pool);
        this.userId = null;
    }

    private async onMessage(data: WebSocket.MessageEvent) {
        const req = JSON.parse(data.data.toString()) as IWebSocketRequest<any>;

        if (req === null || req === undefined) {
            return;
        }

        if (req.request !== "auth" && !this.authorized) {
            console.log(`Received unauthorized request: ${req.request}`);
            return;
        }

        switch (req.request) {
            case "subscribe":
                await this.subscribe(req);
                break;

            case "unsubscribe":
                this.unsubscribe(req);
                break;

            case "auth":
                this.authorized = await this.auth(req);
                break;

            default:
                console.log(`Invalid request: ${req.request}`);
                break;
        }
    }

    private async auth(req: IWebSocketRequest<string>) {
        return await this.verifyRequest(req.data);
    }

    private unsubscribe(req: IWebSocketRequest<ISensorAuthRequest>) {
        if (!this.sensors.has(req.data.sensorId)) {
            return;
        }

        console.log(`Removing sensor: ${req.data.sensorId}`);

        this.sensors.delete(req.data.sensorId);
    }

    private async subscribe(req: IWebSocketRequest<ISensorAuthRequest>) {
        const auth = req.data;

        console.log(`Auth request: ${auth.sensorId} with ${auth.sensorSecret} at ${auth.timestamp}`);

        // ReSharper disable once TsResolvedFromInaccessibleModule
        const date = moment(auth.timestamp).utc(true);
        date.add(WebSocketClient.timeout, "ms");

        // ReSharper disable once TsResolvedFromInaccessibleModule
        if (date.isBefore(moment().utc())) {
            this.socket.close();
            console.log(`Authorization request to late (ID: ${auth.sensorId})`);
        }

        const hash = auth.sensorSecret;
        const sensor = await Sensor.findById(new Types.ObjectId(auth.sensorId));
        auth.sensorSecret = sensor.Secret;

        const computed = createHash("sha256").update(JSON.stringify(auth)).digest("hex");

        if (computed !== hash) {
            const links = await this.client.getSensorLinks(auth.sensorId);
            const match = links.find((value) => {
                return value.UserId === this.userId;
            });

            if (match === null || match === undefined) {
                this.socket.close();
                return;
            }
        }

        this.sensors.set(auth.sensorId, sensor);
        console.log(`Sensor {${auth.sensorId}}{${sensor.Owner}} authorized!`);
    }

    public isServicing(id: string) {
        return this.sensors.has(id);
    }

    public compareSocket(other: WebSocket) {
        return other === this.socket;
    }

    public process(measurements: BulkMeasurementInfo) {
        if (!this.authorized) {
            return;
        }

        const sensor = this.sensors.get(measurements.createdBy.toString());

        if (sensor === null) {
            return;
        }

        const data = JSON.stringify(measurements);
        this.socket.send(data);
    }

    public async authorize(token: string) {
        this.authorized = await this.verifyRequest(token);
    }

    public isAuthorized() {
        return this.authorized;
    }

    private verifyRequest(token: string) {
        if (token === null || token === undefined) {
            return false;
        }

        return new Promise<boolean>((resolve) => {
            jwt.verify(token, this.secret, (err, obj: any) => {
                if (err) {
                    resolve(false);
                    return;
                }

                this.userId = obj.sub;
                resolve(true);
            });
        });
    }
}