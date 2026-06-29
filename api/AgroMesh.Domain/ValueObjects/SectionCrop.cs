using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.ValueObjects;

public record SectionCrop(CropType CropType, double Hectares, int Percentage);
