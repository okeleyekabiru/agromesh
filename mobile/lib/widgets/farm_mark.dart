import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// The Ilevora mark: three rising field rows (Direction B) on a forest tile.
class IlevoraMark extends StatelessWidget {
  final double size;
  const IlevoraMark({super.key, this.size = 32});

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'Ilevora',
      image: true,
      child: SizedBox(width: size, height: size, child: CustomPaint(painter: _IlevoraMarkPainter())),
    );
  }
}

class _IlevoraMarkPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final k = size.width / 96;
    canvas.drawRRect(
      RRect.fromRectAndRadius(Offset.zero & size, Radius.circular(22 * k)),
      Paint()..color = IlevoraColors.forest,
    );
    void row(double y0, List<double> c, double x1, double y1, Color color) {
      final path = Path()
        ..moveTo(14 * k, y0 * k)
        ..cubicTo(c[0] * k, c[1] * k, c[2] * k, c[3] * k, x1 * k, y1 * k);
      canvas.drawPath(
        path,
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeCap = StrokeCap.round
          ..strokeWidth = 7 * k
          ..color = color,
      );
    }
    row(74, [34, 74, 52, 62], 82, 30, const Color(0xFF138A44));
    row(60, [30, 60, 44, 50], 66, 26, const Color(0xFF8FD9A8));
    row(46, [26, 46, 36, 38], 50, 22, const Color(0xFFF5C90C));
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

/// A farm's own logo on a white tile, or the Ilevora mark when there are no bytes (or they cannot be decoded). Used
/// for previewing the logo in the farm settings; the app header always shows the Ilevora mark.
class FarmMarkImage extends StatelessWidget {
  final Uint8List? bytes;
  final double size;
  const FarmMarkImage({super.key, required this.bytes, this.size = 32});

  @override
  Widget build(BuildContext context) {
    final b = bytes;
    if (b == null || b.isEmpty) return IlevoraMark(size: size);
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
          child: BoundedLogoImage(bytes: b, size: size, fallback: () => IlevoraMark(size: size)),
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
