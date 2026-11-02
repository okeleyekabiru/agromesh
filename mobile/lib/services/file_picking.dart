import 'dart:io';
import 'dart:typed_data';
import 'package:file_picker/file_picker.dart';
import 'package:image_picker/image_picker.dart';
import 'upload_rules.dart';

/// A file chosen by the user, read into memory so it can be checked and uploaded.
class PickedUpload {
  final String name;
  final Uint8List bytes;
  const PickedUpload(this.name, this.bytes);
}

/// Where a file can come from. Files = the system file browser (PDFs and images), gallery = photos, camera = a new photo.
enum PickSource { files, gallery, camera }

/// Thin wrapper over image_picker / file_picker so the screens stay testable. Returns null if the user cancels.
/// Errors from the plugins (denied camera permission, no camera) are thrown as [PickFailure] with a readable message.
class FilePicking {
  FilePicking._();

  static final _images = ImagePicker();

  /// [shrink] resizes photos (long side 2048 px) and recompresses them. Use it for logos (2 MB / 4096 px limit);
  /// leave it off for invoice scans so the text stays sharp.
  static Future<PickedUpload?> pick(PickSource source, {bool shrink = false, bool allowPdf = false}) async {
    try {
      switch (source) {
        case PickSource.gallery:
        case PickSource.camera:
          final x = await _images.pickImage(
            source: source == PickSource.camera ? ImageSource.camera : ImageSource.gallery,
            maxWidth: shrink ? 2048 : (source == PickSource.camera ? 3000 : null),
            maxHeight: shrink ? 2048 : (source == PickSource.camera ? 3000 : null),
            imageQuality: shrink ? 90 : (source == PickSource.camera ? 88 : null),
          );
          if (x == null) return null;
          return PickedUpload(x.name, await x.readAsBytes());
        case PickSource.files:
          final f = await FilePicker.pickFile(
            type: FileType.custom,
            allowedExtensions: [if (allowPdf) 'pdf', 'png', 'jpg', 'jpeg', if (shrink) 'webp'],
          );
          if (f == null) return null;
          final path = f.path;
          if (path == null) throw const PickFailure('Could not read that file. Choose another one.');
          final file = File(path);
          // Refuse anything far over the biggest allowed upload before reading it all into memory.
          final length = await file.length();
          if (length > UploadRule.vendorInvoice.maxBytes) {
            throw PickFailure('That file is ${formatBytes(length)}. The limit is ${UploadRule.vendorInvoice.maxText}. Choose a smaller file.');
          }
          return PickedUpload(f.name, await file.readAsBytes());
      }
    } on PickFailure {
      rethrow;
    } catch (e) {
      throw PickFailure(source == PickSource.camera
          ? 'The camera could not be opened. Check that Ilevora may use the camera, or choose a file instead.'
          : 'Could not open the file chooser. Check that Ilevora may read your files and try again.');
    }
  }
}

class PickFailure implements Exception {
  final String message;
  const PickFailure(this.message);
  @override
  String toString() => message;
}
