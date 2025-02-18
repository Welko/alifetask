/**
 * @typedef TextureDescriptor
 * @property {number} width
 * @property {number} height
 * @property {ArrayBufferView} data
 * @property {'nearest' | 'linear'} filter
 * @property {'repeat' | 'clamp'} wrap
 * 
 * @param {WebGL2RenderingContext} gl 
 * @param {TextureDescriptor} descriptor
 * @returns 
 */
export function createTexture(gl, descriptor) {
    const texture = gl.createTexture();
    if (texture === null) {
        throw new Error('Could not create texture');
    }

    if (descriptor.data.byteLength !== descriptor.width * descriptor.height * 4) {
        throw new Error('Data length does not match width and height');
    }

    gl.bindTexture(gl.TEXTURE_2D, texture);

    const level = 0;
    const internalFormat = gl.RGBA;
    const border = 0;
    const format = gl.RGBA;
    const type = gl.UNSIGNED_BYTE;

    gl.texImage2D(
        gl.TEXTURE_2D,
        level,
        internalFormat,
        descriptor.width,
        descriptor.height,
        border,
        format,
        type,
        descriptor.data ?? null
    );

    const filter = descriptor.filter === 'nearest' ? gl.NEAREST : gl.LINEAR;
    const wrap = descriptor.wrap === 'repeat' ? gl.REPEAT : gl.CLAMP_TO_EDGE;

    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, filter);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, filter);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, wrap);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, wrap);
    
    gl.bindTexture(gl.TEXTURE_2D, null);

    return texture;
}

/**
 * @typedef FramebufferDescriptor
 * @property {WebGLTexture} colorAttachment0
 * 
 * @param {WebGL2RenderingContext} gl
 * @param {FramebufferDescriptor} descriptor
 * 
 * @returns {WebGLFramebuffer}
 */
export function createFramebuffer(gl, descriptor) {
    const framebuffer = gl.createFramebuffer();
    if (framebuffer === null) {
        throw new Error('Could not create framebuffer');
    }

    gl.bindFramebuffer(gl.FRAMEBUFFER, framebuffer);
    {
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, descriptor.colorAttachment0, 0);
    }
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);

    return framebuffer;
}

/**
 * @typedef ShaderDescriptor
 * @property {'vertex' | 'fragment'} type
 * @property {string} source
 * 
 * @param {WebGL2RenderingContext} gl 
 * @param {ShaderDescriptor} descriptor 
 * 
 * @returns {WebGLShader}
 */
export function createShader(gl, descriptor) {
    const typeEnum = descriptor.type === 'vertex' ? gl.VERTEX_SHADER : gl.FRAGMENT_SHADER;
    const shader = gl.createShader(typeEnum);
    if (shader === null) {
        throw new Error(`Could not create ${descriptor.type} shader`);
    }

    gl.shaderSource(shader, descriptor.source);

    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        console.error('Error compiling shader:', gl.getShaderInfoLog(shader));
    }

    return shader;
}

/**
 * @typedef ProgramDescriptor
 * @property {WebGLShader} vertexShader
 * @property {WebGLShader} fragmentShader
 * 
 * @param {WebGL2RenderingContext} gl
 * @param {ProgramDescriptor} descriptor
 * @return {WebGLProgram}
 */
export function createProgram(gl, descriptor) {
    const program = gl.createProgram();
    if (program === null) {
        throw new Error('Could not create program');
    }

    gl.attachShader(program, descriptor.vertexShader);
    gl.attachShader(program, descriptor.fragmentShader);

    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        console.error('Error linking program:', gl.getProgramInfoLog(program));
    }

    return program;
}

/**
 * @param {WebGL2RenderingContext} gl
 * @param {WebGLProgram} program
 * @param {string[]} uniformNames
 * @returns {{[uniformName: string]: WebGLUniformLocation}}
 */
export function getUniformLocations(gl, program, uniformNames) {
    return uniformNames.reduce((locations, name) => {
        const location = gl.getUniformLocation(program, name);
        if (location === null) {
            throw new Error(`Could not get uniform location for: ${name}`);
        }
        locations[name] = location;
        return locations;
    }, {});
}