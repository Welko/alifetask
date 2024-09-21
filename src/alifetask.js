import {
    createTexture,
    createFramebuffer,
    createShader,
    createProgram,
    getUniformLocations,
} from "./engine.js";

/**
 * @typedef PassTextures
 * @property {WebGLTexture} population
 * 
 * @typedef Pass
 * @property {PassTextures} textures
 * @property {WebGLFramebuffer} framebuffer
 * 
 * @typedef Program
 * @property {WebGLProgram} program
 * @property {{[uniformName: string]: WebGLUniformLocation}} uniformLocations
 */

export default class ALifeTask {
    
    /**
     * @type {HTMLCanvasElement}
     */
    #canvas;

    /**
     * @type {WebGL2RenderingContext}
     */
    #gl;

    /**
     * @type {null | {simulation: Program, render: Program, brush: Program}}
     */
    #programs;

    /**
     * @type {[Pass, Pass]}
     */
    #pingpong;

    /**
     * @type {null | {x: number, y: number}}
     */
    #brush = null;

    constructor() {
        this.#canvas = document.createElement('canvas');

        const gl = this.#canvas.getContext('webgl2');
        if (gl === null) {
            throw new Error('Could not get WebGL2 context');
        }
        this.#gl = gl;
    }

    get domElement() {
        return this.#canvas;
    }

    /**
     * @param {{width: number, height: number}} size
     */
    async initialize({width, height}) {
        const gl = this.#gl;

        this.#canvas.width = width;
        this.#canvas.height = height;

        const promises = [];

        promises.push(Promise.all([
            fetch('shaders/quad.vs').then((response) => response.text()),
            fetch('shaders/simulation.fs').then((response) => response.text()),
            fetch('shaders/render.fs').then((response) => response.text()),
            fetch('shaders/brush.fs').then((response) => response.text()),
        ]).then(([quadVsSrc, simulationFsSrc, renderFsSrc, brushFsSrc]) => {
            const quadVs = createShader(gl, { type: 'vertex', source: quadVsSrc });
            const simulationFs = createShader(gl, { type: 'fragment', source: simulationFsSrc });
            const renderFs = createShader(gl, { type: 'fragment', source: renderFsSrc });
            const brushFs = createShader(gl, { type: 'fragment', source: brushFsSrc });

            const simulation = createProgram(gl, { vertexShader: quadVs, fragmentShader: simulationFs });
            const render = createProgram(gl, { vertexShader: quadVs, fragmentShader: renderFs });
            const brush = createProgram(gl, { vertexShader: quadVs, fragmentShader: brushFs });

            this.#programs = {
                simulation: {
                    program: simulation,
                    uniformLocations: getUniformLocations(gl, simulation, [
                        'uPopulation',
                    ]),
                },
                render: {
                    program: render,
                    uniformLocations: getUniformLocations(gl, render, [
                        'uPopulation',
                    ]),
                },
                brush: {
                    program: brush,
                    uniformLocations: getUniformLocations(gl, brush, [
                        'uPopulation', 'uMode', 'uStart', 'uEnd', 'uRadius',
                    ]),
                },
            };
        }));

        this.#pingpong = /** @type {[Pass, Pass]} */ ([undefined, undefined].map(() => {
            gl.activeTexture(gl.TEXTURE0 + 0);
            const population = createTexture(gl, {
                width,
                height,
            });

            const framebuffer = createFramebuffer(gl, {
                colorAttachment0: population,
            });

            /**
             * @type {Pass}
             */
            const pass = {
                textures: {
                    population,
                },
                framebuffer,
            };

            return pass;
        }));

        return Promise.all(promises);
    }

    #swap() {
        [this.#pingpong[0], this.#pingpong[1]] = [this.#pingpong[1], this.#pingpong[0]];
    }

    update() {
        if (this.#programs === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass, writePass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, writePass.framebuffer);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.simulation.program);
        gl.uniform1i(this.#programs.simulation.uniformLocations.uPopulation, 0);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.population);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

        this.#swap();
    }

    render() {
        if (this.#programs === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass, writePass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.render.program);
        gl.uniform1i(this.#programs.render.uniformLocations.uPopulation, 0);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.population);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    }

    /**
     * @typedef BrushParams
     * @property {'down' | 'move' | 'up'} action
     * @property {'draw' | 'erase'} mode
     * @property {number} x
     * @property {number} y
     * @property {number} radius
     * 
     * @param {BrushParams} params
     */
    brush(params) {
        if (this.#programs === null) {
            return;
        }

        if (this.#brush === null && params.action !== 'down') {
            return;
        }

        const gl = this.#gl;

        const modeEnum = {draw:0, erase:1}[params.mode];
        const endX = params.x;
        const endY = this.#canvas.height - params.y;
        const startX = this.#brush?.x ?? endX;
        const startY = this.#brush?.y ?? endY;

        const [readPass, writePass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, writePass.framebuffer);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.brush.program);
        gl.uniform1i(this.#programs.brush.uniformLocations.uPopulation, 0);
        gl.uniform1i(this.#programs.brush.uniformLocations.uMode, modeEnum);
        gl.uniform2f(this.#programs.brush.uniformLocations.uStart, startX, startY);
        gl.uniform2f(this.#programs.brush.uniformLocations.uEnd, endX, endY);
        gl.uniform1f(this.#programs.brush.uniformLocations.uRadius, params.radius);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.population);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

        this.#brush = params.action === 'up' ? null : {x: endX, y: endY};

        console.log(params.action);

        this.#swap();
    }

}