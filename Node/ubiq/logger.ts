// Simple logger with timestamps for Ubiq

function timestamp (): string {
    return new Date().toISOString()
}

export const logger = {
    log: (...args: any[]): void => {
        console.log(`[${timestamp()}]`, ...args)
    },
    error: (...args: any[]): void => {
        console.error(`[${timestamp()}]`, ...args)
    },
    warn: (...args: any[]): void => {
        console.warn(`[${timestamp()}]`, ...args)
    },
    info: (...args: any[]): void => {
        console.info(`[${timestamp()}]`, ...args)
    }
}
