/* global process */
const target = process.env['QUIZAPP_API_TARGET'];
if (!target) throw new Error('Set QUIZAPP_API_TARGET to the development API origin before starting Angular.');
module.exports = { '/api/**': { target, changeOrigin: true } };
