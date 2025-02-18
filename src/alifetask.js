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
 * 
 * @typedef ColorMapping
 * @property {number} x
 * @property {`#${string}`} value
 * 
 * @typedef RenderData
 * @property {WebGLTexture} transferFunction
 * @property {number} lightIntensity
 * @property {[number, number, number]} lightColor
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
     * @type {RenderData}
     */
    #renderData;

    /**
     * @type {null | {simulation: Program, render: Program, brush: Program}}
     */
    #programs = null;

    /**
     * @type {null | [Pass, Pass]}
     */
    #pingpong = null;

    /**
     * @type {null | {down: boolean, x: number, y: number}}
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

        const transferFunction = createTexture(gl, {
            width: 256,
            height: 1,
            data: new Uint8Array(256 * 4),
            filter: 'linear',
            wrap: 'clamp',
        });

        this.#renderData = {
            transferFunction,
            lightIntensity: 0,
            lightColor: [0, 0, 0],
        };

        this.scheme = [];
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
     * @param {ColorMapping[]} scheme
     */
    set scheme(scheme) {
        const gl = this.#gl;

        if (scheme.length === 0) {
            scheme = [
                {x: 0, value: '#000000'},
                {x: 1, value: '#ffffff'}
            ];
        } else if (scheme.length === 1) {
            scheme.push(scheme[0]);
        }

        const stringToNumber = (value) => parseInt(value.slice(1), 16);
        
        const data = new Uint8Array(256 * 4);
        for (let i = 0; i < 256; i++) {
            const x = i / 255;

            let nextIndex = scheme.findIndex(({x: x0}) => x0 > x);
            const prevIndex = nextIndex === -1 ? scheme.length - 1 : nextIndex - 1;
            nextIndex = nextIndex === -1 ? prevIndex : nextIndex;

            const {x: x0, value: value0str} = scheme[prevIndex];
            const {x: x1, value: value1str} = scheme[nextIndex];

            const value0 = stringToNumber(value0str);
            const value1 = stringToNumber(value1str);

            const [r0, g0, b0] = [value0 >> 16, value0 >> 8 & 0xff, value0 & 0xff];
            const [r1, g1, b1] = [value1 >> 16, value1 >> 8 & 0xff, value1 & 0xff];

            const t = prevIndex === nextIndex ? 0 : (x - x0) / (x1 - x0);

            data[i * 4 + 0] = r0 + t * (r1 - r0);
            data[i * 4 + 1] = g0 + t * (g1 - g0);
            data[i * 4 + 2] = b0 + t * (b1 - b0);
            data[i * 4 + 3] = 255;
        }

        gl.bindTexture(gl.TEXTURE_2D, this.#renderData.transferFunction);
        gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, 256, 1, gl.RGBA, gl.UNSIGNED_BYTE, data);
    }

    /**
     * @param {{color: `#${string}`, intensity: number}} light
     */
    set light({ color, intensity }) {
        const r = parseInt(color.slice(1, 3), 16) / 255;
        const g = parseInt(color.slice(3, 5), 16) / 255;
        const b = parseInt(color.slice(5, 7), 16) / 255;
        this.#renderData = {...this.#renderData, lightColor: [r, g, b], lightIntensity: intensity};
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
                        'uData0', 'uTransferFunction', 'uMouse', 'uLightIntensity', 'uLightColor'
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
                filter: 'nearest',
                wrap: 'repeat',
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

        const mouseX = this.#brush?.x ?? -1;
        const mouseY = this.#brush?.y ?? -1;

        const [readPass] = this.#pingpong;

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.render.program);
        gl.uniform1i(this.#programs.render.uniformLocations.uData0, 0);
        gl.uniform1i(this.#programs.render.uniformLocations.uTransferFunction, 1);
        gl.uniform2f(this.#programs.render.uniformLocations.uMouse, mouseX, mouseY);
        gl.uniform1f(this.#programs.render.uniformLocations.uLightIntensity, this.#renderData.lightIntensity);
        gl.uniform3fv(this.#programs.render.uniformLocations.uLightColor, this.#renderData.lightColor);

        gl.activeTexture(gl.TEXTURE0 + 0);
        gl.bindTexture(gl.TEXTURE_2D, readPass.textures.data0);

        gl.activeTexture(gl.TEXTURE0 + 1);
        gl.bindTexture(gl.TEXTURE_2D, this.#renderData.transferFunction);

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

        const gl = this.#gl;

        const modeEnum = {draw:0, erase:1}[params.mode];
        const endX = params.x;
        const endY = this.#canvas.height - params.y;
        const startX = this.#brush?.x ?? endX;
        const startY = this.#brush?.y ?? endY;

        if (params.action === 'down') {
            const a = 0;
        }
        
        this.#brush = {x: endX, y: endY, down: this.#brush?.down || params.action === 'down'};
        if (params.action === 'up' || !this.#brush.down) {
            this.#brush.down = false;
            return;
        }
        this.#brush.down = true;

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