using Ilevora.Domain.Enums;

namespace Ilevora.Domain.ValueObjects;

public record BlockCrop(CropType CropType, double Hectares, int Percentage);
