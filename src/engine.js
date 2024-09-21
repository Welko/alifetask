/**
 * @typedef TextureDescriptor
 * @property {number} width
 * @property {number} height
 * @property {ArrayBufferView} [data]
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

    gl.bindTexture(gl.TEXTURE_2D, texture);

    const level = 0;
    const internalFormat = gl.RGBA8;
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

    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    
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
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, descriptor.colorAttachment0.gpu, 0);
    }
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);

    return framebuffer;
}