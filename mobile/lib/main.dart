import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'theme/app_theme.dart';
import 'stores/app_store.dart';
import 'screens/main_shell.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const AgroMeshApp());
}

class AgroMeshApp extends StatelessWidget {
  const AgroMeshApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => AppStore()..init(),
      child: MaterialApp(
        title: 'AgroMesh',
        theme: AgroTheme.theme,
        debugShowCheckedModeBanner: false,
        home: const MainShell(),
      ),
    );
  }
}
