import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../stores/auth_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

/// Mobile does login only — accounts, companies and farms are created on the
/// web app. This screen just authenticates against the existing backend and
/// hands off to the farm picker / main shell once a session is established.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _obscure = true;

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    await context.read<AuthStore>().login(_emailCtrl.text.trim(), _passwordCtrl.text);
    // Success/failure is reflected via AuthStore.status/.error — AppGate reacts
    // to the status change, and the error message (if any) renders below.
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthStore>();

    return Scaffold(
      backgroundColor: IlevoraColors.mist,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 380),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 56, height: 56,
                      alignment: Alignment.center,
                      decoration: BoxDecoration(color: IlevoraColors.canopy, borderRadius: BorderRadius.circular(14)),
                      child: const Text('A', style: TextStyle(fontFamily: 'PlusJakartaSans', color: IlevoraColors.palm, fontWeight: FontWeight.w800, fontSize: 26)),
                    ),
                    const SizedBox(height: 20),
                    Text('Ilevora', textAlign: TextAlign.center, style: Theme.of(context).textTheme.displayMedium),
                    const SizedBox(height: 4),
                    const Text('Field supervisor login', textAlign: TextAlign.center,
                      style: TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, color: IlevoraColors.stone)),
                    const SizedBox(height: 32),

                    AgroTextField(
                      label: 'Email',
                      hint: 'you@farm.com',
                      controller: _emailCtrl,
                      keyboardType: TextInputType.emailAddress,
                      validator: (v) => (v == null || !v.contains('@')) ? 'Enter a valid email' : null,
                    ),
                    const SizedBox(height: 16),
                    AgroTextField(
                      label: 'Password',
                      hint: '••••••••',
                      controller: _passwordCtrl,
                      obscureText: _obscure,
                      validator: (v) => (v == null || v.isEmpty) ? 'Enter your password' : null,
                      suffixIcon: IconButton(
                        tooltip: _obscure ? 'Show password' : 'Hide password',
                        icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined, size: 22, color: IlevoraColors.stone),
                        onPressed: () => setState(() => _obscure = !_obscure),
                      ),
                    ),

                    if (auth.error != null) ...[
                      const SizedBox(height: 14),
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: IlevoraColors.alert.withValues(alpha: 0.08),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: IlevoraColors.alert.withValues(alpha: 0.25)),
                        ),
                        child: Text(auth.error!, style: const TextStyle(color: IlevoraColors.alert, fontSize: 13)),
                      ),
                    ],

                    const SizedBox(height: 24),
                    ElevatedButton(
                      onPressed: auth.isSubmitting ? null : _submit,
                      child: auth.isSubmitting
                        ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : const Text('Log In'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
