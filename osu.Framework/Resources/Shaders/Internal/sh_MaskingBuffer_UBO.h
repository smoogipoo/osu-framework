MaskingInfo g_MaskingInfo;

layout(std140, set = -2, binding = 0) uniform g_MaskingBuffer
{
    MaskingInfo Data[128];
} MaskingBuffer;

void InitMasking(int index)
{
    g_MaskingInfo = MaskingBuffer.Data[index];
}