import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'theme/app_theme.dart';
import 'stores/app_store.dart';
import 'stores/auth_store.dart';
import 'screens/app_gate.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const AgroMeshApp());
}

class AgroMeshApp extends StatelessWidget {
  const AgroMeshApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthStore()..bootstrap()),
        ChangeNotifierProvider(create: (_) => AppStore()),
      ],
      child: MaterialApp(
        title: 'AgroMesh',
        theme: AgroTheme.theme,
        debugShowCheckedModeBanner: false,
        home: const AppGate(),
      ),
    );
  }
}
