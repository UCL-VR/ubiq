// These plugins are necessary for Rollup to work. Make sure they are installed
// like so:
//	npm install --save-dev @rollup/plugin-node-resolve 
//	npm install --save-dev rollup-plugin-polyfill-node
//	npm install --save-dev rollup-plugin-typescript2
//	npm install --save-dev rollup-plugin-polyfill-node

import externalGlobals from "rollup-plugin-external-globals";
import { nodeResolve } from '@rollup/plugin-node-resolve'
import typescript from 'rollup-plugin-typescript2'
import nodePolyfills from 'rollup-plugin-polyfill-node'

// The following configuration file outputs the bundle as an ES Module.
// The nodeResolve and nodePolyfills plugins create browser compatible
// equivalents to node modules.
// The externalGlobals is used to tell Rollup that some types are available
// in the browser, in this case, the WebSocket class, so they shouldn't be
// bundled.

export default {
	input: 'lib.ts',
	output: {
		file: '../Browser/bundle.js',
		format: 'es',
	},
	plugins: [
		typescript(),
		externalGlobals({
			ws: "WebSocket"
		}),
		nodeResolve(),
		nodePolyfills()
	]
};