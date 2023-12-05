import type { JestConfigWithTsJest } from 'ts-jest'

// This is to allow self-signed certificates to be used during tests that make
// WebSocket connections
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'

const config: JestConfigWithTsJest = {
    verbose: true,
    transform: {
    // '^.+\\.[tj]sx?$' to process js/ts with `ts-jest`
    // '^.+\\.m?[tj]sx?$' to process js/ts/mjs/mts with `ts-jest`
        '^.+\\.tsx?$': [
            'ts-jest',
            {
                // ts-jest configuration goes here
            }
        ]
    },
    resolver: 'ts-jest-resolver'

}

export default config
