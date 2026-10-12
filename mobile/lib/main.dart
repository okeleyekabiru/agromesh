import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:sqflite/sqflite.dart';
import 'package:sqflite_common_ffi_web/sqflite_ffi_web.dart';
import 'package:provider/provider.dart';
import 'theme/app_theme.dart';
import 'stores/app_store.dart';
import 'stores/auth_store.dart';
import 'screens/app_gate.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // In a browser there is no native sqlite: use the WebAssembly one (stored in the browser's IndexedDB).
  if (kIsWeb) databaseFactory = databaseFactoryFfiWebNoWebWorker;
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
