import {
    createTexture,
    createFramebuffer,
} from "./engine.js";

/**
 * @typedef PassTextures
 * @property {WebGLTexture} population
 * 
 * @typedef Pass
 * @property {PassTextures} textures
 * @property {WebGLFramebuffer} framebuffer
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
     * @typedef {[Pass, Pass]}
     */
    #pingpong;

    constructor(width, height) {
        this.#canvas = document.createElement('canvas');
        this.#canvas.width = width;
        this.#canvas.height = height;

        const gl = this.#canvas.getContext('webgl2');
        if (gl === null) {
            throw new Error('Could not get WebGL2 context');
        }
        this.#gl = gl;

        this.#initialize({width, height});
    }

    get domElement() {
        return this.#canvas;
    }

    /**
     * @param {{width: number, height: number}} size
     */
    #initialize({width, height}) {
        const gl = this.#gl;

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
    }

}