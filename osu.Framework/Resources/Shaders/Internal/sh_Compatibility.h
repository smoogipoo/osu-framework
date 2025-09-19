// This file is automatically included in every shader.

#ifndef INTERNAL_COMPATIBILITY_H
#define INTERNAL_COMPATIBILITY_H

#version 450
#extension GL_ARB_uniform_buffer_object : enable

#ifdef OSU_IS_VERTEX_SHADER
#define UNIFORM_TEXTURE_RESOURCE_SET 0
#define UNIFORM_BUFFER_RESOURCE_SET 1
#else
#define UNIFORM_TEXTURE_RESOURCE_SET 2
#define UNIFORM_BUFFER_RESOURCE_SET 3
#endif

#endif
