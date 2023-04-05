#extension GL_ARB_shader_storage_buffer_object : enable

MaskingInfo g_MaskingInfo;

layout(std140, set = -2, binding = 0) readonly buffer g_MaskingBuffer
{
    MaskingInfo Data[];
} MaskingBuffer;

void InitMasking(int index)
{
    g_MaskingInfo = MaskingBuffer.Data[index];
}