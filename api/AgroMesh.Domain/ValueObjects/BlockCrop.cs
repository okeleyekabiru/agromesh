using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.ValueObjects;

public record BlockCrop(CropType CropType, double Hectares, int Percentage);
