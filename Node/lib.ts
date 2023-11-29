// This file is the entry point for the bundler used to build Ubiq classes for
// the Browser with Rollup.

export { NetworkId, NetworkScene, WebSocketConnectionWrapper } from 'ubiq'
export { RoomClient, PeerConnectionManager, AvatarManager, ThreePointTrackedAvatar, LogCollector } from 'components'

// This file is intended to bundle almost all the Ubiq Components and
// dependencies for use in the browser, for the convenience of the Web Samples.

// You may prefer your application be more selective, to reduce the library size.
// To this end, import() the Ubiq resources directly in your Ts file, or create
// an equivalent lib.ts requiring only those classes necessary, and use the
// bundler on this instead.

// Take care that not all classes will be supported on the browser. All the Ubiq
// Peer Components will work, but a number of server-side and connections will
// not. It is not possible to create server sockets in the Browser, for example.

// The rollup config makes use of polyfills to use Node module APIs in the
// browser.
// https://www.npmjs.com/package/rollup-plugin-polyfill-node
// Look at this to get an idea of which classes will and will not work based on
// which modules they use.

// Take care as well that some npm packages will bring in dependencies. For
// example, ws brings in bufferutil. This works OK on Node but when bundling for
// the browser, the bundle will end up with require('bufferutil') calls (for
// example). Make sure to install such dependencies directly
// (e.g. npm install bufferutil): there should be no require() calls in the
// bundled js.

// To build, give the command
//     npx rollup --config
