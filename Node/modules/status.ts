// The status module serves an HTTP API to provide information about the server
// status, allow gathering of statistics, and performing uptime tests.

import type { RoomServer } from './roomserver'
import https from 'https'
import path from 'path'
import fs from 'fs'
import express, { type Request, type Response } from 'express'
import cookieParser from 'cookie-parser'

// Some calls are protected via API keys. These are UUIDs that should be provided
// in the apikeys member of the config object. If no API keys are defined, then
// the API is available to everyone.

interface StatusConfig {
    port: number
    cert: string
    key: string
    apikeys: string[]
}

export class Status {
    roomServer: RoomServer
    apikeys: string[]

    constructor (roomServer: RoomServer, config: StatusConfig) {
        this.roomServer = roomServer
        this.apikeys = config.apikeys

        const options: any = {}

        try {
            options.key = fs.readFileSync(path.resolve(config.key))
            options.cert = fs.readFileSync(path.resolve(config.cert))
        } catch (error) {
            console.error('Unable to read certificate for status module. You will not be able to create secure connections to the status APIs.')
        }

        const app = express()

        app.use(cookieParser())

        app.get('/', (req, res) => {
            res.send('Ubiq Server Status Module')
        })

        app.get('/favicon.ico', (req, res) => {
            res.sendStatus(404)
        })

        app.get('/stats', (req, res) => {
            const stats = roomServer.getStats()
            const response = JSON.stringify(stats, (key, value) =>
                typeof value === 'bigint' ? value.toString() : value
            )
            res.send(response)
        })

        // Checks whether the server is currently running. This method performs a
        // basic operation (enumerating the rooms) to make sure everything is OK.
        app.get('/isup', (req, res) => {
            if (roomServer.getRooms().length > -1) {
                res.sendStatus(202)
            } else {
                res.sendStatus(501)
            }
        })

        // This method is a convenience API that just tells the agent to put
        // the api key in the cookie. This could also be done manually. API keys
        // can also be provided as query parameters if a truly statless HTTP
        // agent is used.
        app.get('/setapikey/:apikey', (req, res) => {
            if (this.apikeys.includes(req.params.apikey)) {
                res.cookie('apikey', req.params.apikey)
                res.send(202)
            } else {
                res.send(401)
            }
        })

        // Placing the middleware use call here means all following routes will
        // have the API key checked

        app.use(this.checkApiKey.bind(this))

        // Returns a list of rooms (by uuid)
        app.get('/rooms', (req, res) => {
            res.send(roomServer.getRooms().map(room => room.uuid))
        })

        // Returns the details about a specific room, provided a uuid
        app.get('/rooms/:uuid', (req, res) => {
            const room = roomServer.getRoom(req.params.uuid)
            const info = {
                room: room?.getRoomArgs(),
                peers: room?.getPeersArgs()
            }
            res.send(info)
        })

        https.createServer(options, app).listen(config.port, () => {
            console.log(`Added status server on port ${config.port}`)
        })
    }

    checkApiKey (req: Request, res: Response, next: any): void {
        // Are we using API key protection at all?
        if (this.apikeys.length <= 0) {
            next()
            return
        }

        // Is the API key in the query?
        if (req.query.apikey !== undefined) {
            if (this.apikeys.includes(req.query.apikey as string)) {
                next()
                return
            }
        }

        // Or is it in the cookie?
        if (req.cookies !== undefined) {
            if (req.cookies.apikey !== undefined) {
                if (this.apikeys.includes(req.cookies.apikey)) {
                    next()
                    return
                }
            }
        }

        res.statusCode = 401
        res.send('This API requires an API key to call. Set the "apikey" parameter in your cookie, or pass the key')
    }
}
