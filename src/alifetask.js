import {
    createTexture,
    createFramebuffer,
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
     * @type {null | {simulation: Program}}
     */
    #programs;

    /**
     * @typedef {[Pass, Pass]}
     */
    #pingpong;

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
        ]).then(([vs, fs]) => {
            const simulation = createProgram(gl, { vertexSource: vs, fragmentSource: fs });
            this.#programs = {
                simulation: {
                    program: simulation,
                    uniformLocations: getUniformLocations(gl, simulation, [

                    ]),
                }
            };
        }));

        this.#pingpong = [undefined, undefined].map(() => {
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
        });

        return Promise.all(promises);
    }

    /**
     * @param {number} deltaTimeMs 
     */
    update(deltaTimeMs) {
        if (this.#programs === null) {
            return;
        }

        const gl = this.#gl;

        const [readPass, writePass] = this.#pingpong;

        //gl.bindFramebuffer(gl.FRAMEBUFFER, writePass.framebuffer);
        gl.viewport(0, 0, this.#canvas.width, this.#canvas.height);

        gl.useProgram(this.#programs.simulation.program);
        //gl.uniform1i(this.#uniforms.population, 0);

        //gl.activeTexture(gl.TEXTURE0 + 0);
        //gl.bindTexture(gl.TEXTURE_2D, readPass.textures.population);

        gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);

        [this.#pingpong[0], this.#pingpong[1]] = [this.#pingpong[1], this.#pingpong[0]];
    }

}