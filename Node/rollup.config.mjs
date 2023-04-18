
// Though Rollup is designed for ES6, Node uses CommonJs, and therefore so does
// Ubiq. These plugins add CommonJs support to Rollup. Make sure it is installed
// like so: 
//    npm install @rollup/plugin-commonjs --save-dev
//    npm install @rollup/plugin-node-resolve --save-dev
//    npm install --save-dev rollup-plugin-polyfill-node
import commonjs from '@rollup/plugin-commonjs';
import { nodeResolve } from '@rollup/plugin-node-resolve';
import nodePolyfills from 'rollup-plugin-polyfill-node';

export default {
	input: 'lib.js',
	output: {
		file: '../Browser/bundle.js',
		format: 'es'
	},
	plugins: [
		commonjs(),
		nodeResolve(),
		nodePolyfills()
	]
};