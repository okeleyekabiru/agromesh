import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

/// Lightweight mobile "quick add" for a section — name only. Full section
/// setup (crop mix, precise area, etc.) happens on the web app; this just
/// creates a bare section against the active farm so the field team can start
/// tracking it and capture its boundary via GPS. Requires connectivity — this
/// is not part of the offline-resilience story (unlike boundary point capture).
class AddSectionScreen extends StatefulWidget {
  const AddSectionScreen({super.key});

  @override
  State<AddSectionScreen> createState() => _AddSectionScreenState();
}

class _AddSectionScreenState extends State<AddSectionScreen> {
  final _nameCtrl = TextEditingController();
  bool _isSubmitting = false;
  String? _error;

  @override
  void dispose() {
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final name = _nameCtrl.text.trim();
    if (name.isEmpty) {
      setState(() => _error = 'Enter a section name.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
    });

    try {
      await context.read<AppStore>().addSection(name);
      if (mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Section "$name" added'),
            backgroundColor: AgroColors.leaf,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isOnline = context.watch<AppStore>().isOnline;

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: const Text('Add Section')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (!isOnline)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: AgroColors.harvest.withOpacity(0.1), borderRadius: BorderRadius.circular(8)),
                child: const Row(
                  children: [
                    Icon(Icons.cloud_off_outlined, size: 16, color: AgroColors.harvest),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'You are offline. Adding a section requires an internet connection.',
                        style: TextStyle(fontSize: 12),
                      ),
                    ),
                  ],
                ),
              ),
            AgroTextField(label: 'Section Name', hint: 'e.g. North Block', controller: _nameCtrl),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12)),
              ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: (!isOnline || _isSubmitting) ? null : _submit,
              child: _isSubmitting
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Text('Add Section'),
            ),
          ],
        ),
      ),
    );
  }
}
