import perf_hooks from 'perf_hooks'
import { Buffer } from 'buffer' // This import is needed for rollup to polyfill Buffer
import { z } from 'zod'

const performance = perf_hooks.performance

const MESSAGE_HEADER_SIZE = 8

export const NetworkIdSchema = z.object({
    a: z.number().int(),
    b: z.number().int()
})

export interface INetworkId extends z.infer<typeof NetworkIdSchema> {
}

export type NetworkIdObject = Record<string, any> & {
    networkId: LooseNetworkId
}

export type LooseNetworkId = string | number | Buffer | ArrayBufferView | NetworkId | NetworkIdObject

export class NetworkId {
    a: number
    b: number
    constructor (data: LooseNetworkId) {
        if (typeof (data) === 'string') {
            data = data.replace(/-/g, '')
            this.a = parseInt(data.substring(0, 8), 16)
            this.b = parseInt(data.substring(8, 16), 16)
            return
        }
        if (typeof (data) === 'number') {
            this.a = 0
            this.b = data
            return
        }
        if (Buffer.isBuffer(data)) {
            this.a = data.readUInt32LE(0)
            this.b = data.readUInt32LE(4)
            return
        }
        if (ArrayBuffer.isView(data)) {
            const view = new Uint32Array(data.buffer)
            this.a = view[0]
            this.b = view[1]
            return
        }
        if (typeof (data) === 'object' && data.hasOwnProperty('a') && typeof (data.a) === 'number' && data.hasOwnProperty('b') && typeof (data.b) === 'number') {
            this.a = data.a
            this.b = data.b
            return
        }
        // eslint-disable-next-line @typescript-eslint/no-base-to-string, @typescript-eslint/restrict-template-expressions
        throw new Error(`Cannot construct NetworkId from ${data}`)
    }

    toString (): string {
        return `${this.a.toString(16)}-${this.b.toString(16)}`
    }

    static Schema = z.object({
        a: z.number().int(),
        b: z.number().int()
    })

    static Null = { a: 0, b: 0 }

    static Compare (x: NetworkId, y: NetworkId): boolean {
        return (x.a === y.a && x.b === y.b)
    }

    static WriteBuffer (networkId: NetworkId, buffer: Buffer, offset: number): void {
        if (networkId === undefined) {
            console.error('Undefined networkId when writing ' + (new Error().stack))
            return
        }
        buffer.writeUInt32LE(networkId.a, offset + 0)
        buffer.writeUInt32LE(networkId.b, offset + 4)
    }

    static Unique (): NetworkId {
        // eslint-disable-next-line @typescript-eslint/strict-boolean-expressions
        let d2 = (performance?.now && (performance.now() * 1000)) || 0// Time in microseconds since page-load or 0 if unsupported
        const id = 'xxxx-xxxx-xxxx-xxxx'.replace(/[xy]/g, function (c) {
            let r = Math.random() * 16// random number between 0 and 16
            r = (d2 + r) % 16 | 0
            d2 = Math.floor(d2 / 16)
            return r.toString(16)
        })
        return new NetworkId(id)
    }

    static Valid (x: NetworkId): boolean {
        return x.a !== 0 && x.b !== 0
    }

    static Create (namespace: LooseNetworkId, service: string | number | NetworkId): NetworkId {
        const id = new NetworkId(namespace)
        const data = new Uint32Array(2)
        data[0] = id.a
        data[1] = id.b
        if (typeof (service) === 'number') {
            data[0] = Math.imul(data[0], service)
            data[1] = Math.imul(data[1], service)
            return new NetworkId(data)
        } else if (service instanceof NetworkId) {
            data[0] = Math.imul(data[0], service.b)
            data[1] = Math.imul(data[1], service.a)
            return new NetworkId(data)
        } else if (typeof (service) === 'string') {
            const bytes = Buffer.from(service, 'utf8')
            for (let i = 0; i < bytes.length; i++) {
                if (i % 2 !== 0) {
                    data[0] = Math.imul(data[0], bytes[i])
                } else {
                    data[1] = Math.imul(data[1], bytes[i])
                }
            }
            return new NetworkId(data)
        }
        throw new Error(`Cannot construct namespaced NetworkId from ${typeof (namespace)} and ${typeof (service)}`)
    }
}

interface IMessage {
    buffer: Buffer
    length: number
    networkId: NetworkId
    message: Buffer
}

export class Message implements IMessage {
    buffer: Buffer = Buffer.from('')
    length: number = -1
    networkId: NetworkId = new NetworkId(-1)
    message: Buffer = Buffer.from('')

    static Wrap (data: Buffer): Message {
        const msg = new Message()
        msg.buffer = data
        msg.length = data.readInt32LE(0)
        msg.networkId = new NetworkId(data.slice(4))
        msg.message = data.slice(12)
        return msg
    }

    static Create (networkId: NetworkId | string, message: string | object | Buffer): Message {
        let convertedMessage: Buffer = Buffer.from('')
        if (!Buffer.isBuffer(message)) {
            if (typeof (message) === 'object') {
                convertedMessage = Buffer.from(JSON.stringify(message), 'utf8')
            }
            if (typeof (message) === 'string') {
                convertedMessage = Buffer.from(message, 'utf8')
            }
        } else {
            convertedMessage = message
        }
        const length = convertedMessage.length + MESSAGE_HEADER_SIZE
        const buffer = Buffer.alloc(length + 4)

        if (typeof (networkId) === 'string') {
            networkId = new NetworkId(networkId)
        }

        buffer.writeInt32LE(length, 0)
        NetworkId.WriteBuffer(networkId, buffer, 4)
        convertedMessage.copy(buffer, 12)

        const msg = new Message()
        msg.buffer = buffer
        msg.length = length
        msg.networkId = networkId
        msg.message = convertedMessage

        return msg
    }

    toString (): string {
        return new TextDecoder().decode(this.message)
    }

    toObject (): any {
        return JSON.parse(this.toString())
    }

    toBuffer (): Buffer {
        return this.message
    }
}
