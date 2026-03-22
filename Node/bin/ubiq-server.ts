#!/usr/bin/env node

import { fileURLToPath } from 'url'
import path from 'path'
import { WrappedSecureWebSocketServer, WrappedTcpServer } from '@ucl-vr/ubiq'
import { RoomServer, IceServerProvider, Status } from 'modules'
import nconf from 'nconf'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
// At runtime __dirname is dist/bin/, so go up two levels to reach the package root
const packageRoot = path.resolve(__dirname, '..', '..')

// nconf loads the configuration hierarchically - settings that load *first*
// take priority. CLI arguments and local config files override defaults.
process.argv.slice(2).forEach(element => {
    nconf.file(element, element)
})
nconf.file('local', path.join(process.cwd(), 'config', 'local.json'))
nconf.file('default', path.join(packageRoot, 'config', 'default.json'))

const roomServer = new RoomServer()
roomServer.addServer(new WrappedTcpServer(nconf.get('roomserver:tcp')))
roomServer.addServer(new WrappedSecureWebSocketServer(nconf.get('roomserver:wss')))

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const statusModule = new Status(roomServer, nconf.get('status'))

const iceServerProvider = new IceServerProvider(roomServer)
const iceServers = nconf.get('iceservers')
if (iceServers !== undefined) {
    for (const iceServer of iceServers) {
        iceServerProvider.addIceServer(
            iceServer.uri,
            iceServer.secret,
            iceServer.timeoutSeconds,
            iceServer.refreshSeconds,
            iceServer.username,
            iceServer.password)
    }
}

const roomTypeName = nconf.get('roomserver:roomType')
if (roomTypeName !== undefined) {
    // eslint-disable-next-line no-eval
    roomServer.T = eval(roomTypeName)
}

process.on('SIGINT', function () {
    roomServer.exit().then(() => {
        console.log('Shutdown')
    }).catch((error) => {
        console.error(error)
    }).finally(() => {
        process.exit(0)
    })
})
