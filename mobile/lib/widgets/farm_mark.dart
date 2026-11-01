import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// The AgroMesh "A" mark.
class AgroMark extends StatelessWidget {
  final double size;
  const AgroMark({super.key, this.size = 32});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: AgroColors.canopy, borderRadius: BorderRadius.circular(size / 4)),
      child: Center(
        child: Text('A', style: TextStyle(fontFamily: 'PlusJakartaSans', color: AgroColors.palm, fontWeight: FontWeight.w800, fontSize: size / 2)),
      ),
    );
  }
}

/// A farm's own logo on a white tile, or the AgroMesh mark when there are no bytes (or they cannot be decoded). Used
/// for previewing the logo in the farm settings; the app header always shows the AgroMesh mark.
class FarmMarkImage extends StatelessWidget {
  final Uint8List? bytes;
  final double size;
  const FarmMarkImage({super.key, required this.bytes, this.size = 32});

  @override
  Widget build(BuildContext context) {
    final b = bytes;
    if (b == null || b.isEmpty) return AgroMark(size: size);
    return Semantics(
      label: 'Farm logo',
      image: true,
      child: Container(
        width: size,
        height: size,
        padding: EdgeInsets.all(size / 14),
        decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(size / 4)),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(size / 6),
          child: BoundedLogoImage(bytes: b, size: size, fallback: () => AgroMark(size: size)),
        ),
      ),
    );
  }
}

/// A logo image decoded no larger than the box it is shown in. A 4096 x 4096 logo would otherwise stay in memory at
/// full size (about 64 MB) to draw a 32 dp mark.
class BoundedLogoImage extends StatelessWidget {
  final Uint8List bytes;
  final double size;
  final Widget Function() fallback;
  const BoundedLogoImage({super.key, required this.bytes, required this.size, required this.fallback});

  @override
  Widget build(BuildContext context) {
    final px = (size * MediaQuery.devicePixelRatioOf(context)).ceil().clamp(1, 1024);
    return Image(
      image: ResizeImage(MemoryImage(bytes), width: px, height: px, policy: ResizeImagePolicy.fit),
      fit: BoxFit.contain,
      gaplessPlayback: true,
      errorBuilder: (_, __, ___) => fallback(),
    );
  }
}
