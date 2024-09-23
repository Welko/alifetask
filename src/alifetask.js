import {
    createTexture,
    createFramebuffer,
    createShader,
    createProgram,
    getUniformLocations,
} from "./engine.js";

/**
 * @typedef PassTextures
 * @property {WebGLTexture} data0
 * 
 * @typedef Pass
 * @property {PassTextures} textures
 * @property {WebGLFramebuffer} framebuffer
 * 
 * @typedef Program
 * @property {WebGLProgram} program
 * @property {{[uniformName: string]: WebGLUniformLocation}} uniformLocations
 * 
 * @typedef SimulationParams
 * @property {number} feedRate
 * @property {number} killRate
 * @property {number} diffusionRateU
 * @property {number} diffusionRateV
 * @property {number} deltaTime
 */

export default class ALifeTask {

    /**
     * @type {SimulationParams}
     */
    #params = {
        feedRate: 0.035,
        killRate: 0.065,
        diffusionRateU: 0.16,
        diffusionRateV: 0.08,
        deltaTime: 0.5,
    };
    
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
    #programs = null;

    /**
     * @type {null | [Pass, Pass]}
     */
    #pingpong = null;

    /**
     * @type {null | {x: number, y: number}}
     */
    #brush = null;

    /**
     * 
     * @param {SimulationParams} params 
     */
    constructor(params) {
        this.#params = params;
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
     * @param {Partial<SimulationParams>} params
     */
    set params(params) {
        this.#params = {...this.#params, ...params};
    }

    /**
     * @param {{width: number, height: number}} size
     */
    async initialize({width, height}) {
        const gl = this.#gl;

        this.#canvas.width = width;
        this.#canvas.height = height;
        this.#canvas.style.width = `${width}px`;
        this.#canvas.style.height = `${height}px`;

        if (this.#pingpong !== null) {
            this.#pingpong.forEach(pass => {
                gl.deleteFramebuffer(pass.framebuffer);
                gl.deleteTexture(pass.textures.data0);
            });
        }

        if (this.#programs !== null) {
            Object.values(this.#programs).forEach(({program}) => {
                gl.deleteProgram(program);
            });
        }

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
                        'uData0', 'uFeedRate', 'uKillRate', 'uDiffusionRateU', 'uDiffusionRateV', 'uDeltaTime'
                    ]),
                },
                render: {
                    program: render,
                    uniformLocations: getUniformLocations(gl, render, [
                        'uData0',
                    ]),
                },
                brush: {
                    program: brush,
                    uniformLocations: getUniformLocations(gl, brush, [
                        'uData0', 'uMode', 'uStart', 'uEnd', 'uRadius', 'uColor'
                    ]),
                },
            };
        }));
        
        this.#pingpong = /** @type {[Pass, Pass]} */ ([undefined, undefined].map(() => {
            // All zeros
            const numChannels = 4;
            const initialTextureData = new Uint8Array(width * height * numChannels);
            
            gl.activeTexture(gl.TEXTURE0 + 0);
            const populationTexture = createTexture(gl, {
                width,
                height,
                data: initialTextureData,
            });

            const framebuffer = createFramebuffer(gl, {
                colorAttachment0: populationTexture,
            });

            /**
             * @type {Pass}
             */
            const pass = {
                textures: {
                    data0: populationTexture,
                },
                framebuffer,
            };

            return pass;
        }));

        return Promise.all(promises);
    }

    #swap() {
        if (this.#pingpong === null) {
            return;
        }
        [this.#pingpong[0], this.#pingpong[1]] = [this.#pingpong[1], this.#pingpong[0]];
    }

    update() {
        if (this.#programs === null || this.#pingpong === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass, writePass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, writePass.framebuffer);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);
        gl.clear(gl.COLOR_BUFFER_BIT);

        gl.useProgram(this.#programs.simulation.program);
        gl.uniform1i(this.#programs.simulation.uniformLocations.uData0, 0);
        gl.uniform1f(this.#programs.simulation.uniformLocations.uFeedRate, this.#params.feedRate);
        gl.uniform1f(this.#programs.simulation.uniformLocations.uKillRate, this.#params.killRate);
        gl.uniform1f(this.#programs.simulation.uniformLocations.uDiffusionRateU, this.#params.diffusionRateU);
        gl.uniform1f(this.#programs.simulation.uniformLocations.uDiffusionRateV, this.#params.diffusionRateV);
        gl.uniform1f(this.#programs.simulation.uniformLocations.uDeltaTime, this.#params.deltaTime);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.data0);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

        this.#swap();
    }

    render() {  
        if (this.#programs === null || this.#pingpong === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.render.program);
        gl.uniform1i(this.#programs.render.uniformLocations.uData0, 0);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.data0);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    }

    /**
     * @typedef BrushParams
     * @property {'down' | 'move' | 'up'} action
     * @property {'draw' | 'erase'} mode
     * @property {number} x
     * @property {number} y
     * @property {number} radius
     * @property {'R' | 'G'} channel Ignored if mode is 'erase'
     * @property {number} intensity Range: [0, 255]
     * 
     * @param {BrushParams} params
     */
    brush(params) {
        if (this.#programs === null || this.#pingpong === null) {
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
        
        this.#brush = params.action === 'up' ? null : {x: endX, y: endY};
        if (params.action === 'up') {
            return;
        }

        const color = [
            params.channel === 'R' ? params.intensity / 255 : 0,
            params.channel === 'G' ? params.intensity / 255: 0,
            0,
            0,
        ];

        const [readPass, writePass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, writePass.framebuffer);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);
        gl.clear(gl.COLOR_BUFFER_BIT);

        gl.useProgram(this.#programs.brush.program);
        gl.uniform1i(this.#programs.brush.uniformLocations.uData0, 0);
        gl.uniform1i(this.#programs.brush.uniformLocations.uMode, modeEnum);
        gl.uniform2f(this.#programs.brush.uniformLocations.uStart, startX, startY);
        gl.uniform2f(this.#programs.brush.uniformLocations.uEnd, endX, endY);
        gl.uniform1f(this.#programs.brush.uniformLocations.uRadius, params.radius);
        gl.uniform4fv(this.#programs.brush.uniformLocations.uColor, color);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.data0);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

        this.#swap();
    }

    /**
     * @typedef FillParams
     * @property {'R' | 'G'} channel
     * @property {'uniform' | 'random'} mode
     * @property {number} intensity Range: [0, 255]
     * @property {number} threshold Range: [0, 1] - Ignored if mode is 'uniform'
     * 
     * @param {FillParams} params
     */
    fill(params) {
        if (this.#pingpong === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass, writePass] = this.#pingpong;

        // Read data from the read pass
        const data = new Uint8Array(this.#canvas.width * this.#canvas.height * 4);
        gl.bindFramebuffer(gl.FRAMEBUFFER, readPass.framebuffer);
        gl.readPixels(0, 0, this.#canvas.width, this.#canvas.height, gl.RGBA, gl.UNSIGNED_BYTE, data);

        // Fill data
        const offset = (params.channel === 'R' ? 0 : 1);
        const v = params.mode === 'uniform'
            ? () => params.intensity
            : () => Math.random() < params.threshold ? params.intensity : 0;
        for (let i = 0; i < data.length; i += 4) {
            data[i + offset] = v();
        }

        // Write data to the write pass using texSubImage2D
        gl.bindTexture(gl.TEXTURE_2D, writePass.textures.data0);
        gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, this.#canvas.width, this.#canvas.height, gl.RGBA, gl.UNSIGNED_BYTE, data);

        this.#swap();
    }

}