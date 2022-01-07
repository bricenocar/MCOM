const path = require('path');

module.exports = {
  entry: {
    migrate: './scripts.v2/migrate.js',
    capture: './scripts.v2/capture.js',
    cleanup: './scripts.v2/cleanup.js',
    generate: './scripts.v2/generate.js',
    utils: './scripts.v2/utils.js'
  },
  output: {
    filename: '[name].js',
    path: __dirname + '/dist',
  },
  optimization: {
    minimize: false
  },
  resolve: {
    mainFields: ['main']
  },
  target: 'node',
  module: {
    rules: [
      {
        type: 'javascript/auto',
        test: /\.mjs$/,
        use: []
      }
    ]
  }
};