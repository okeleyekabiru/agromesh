import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../stores/auth_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

/// Shown when the logged-in user has more than one farm. Mobile never creates
/// farms — it only lets the user pick which existing (web-created) farm to
/// work with as their "active farm" for this device.
class SelectFarmScreen extends StatelessWidget {
  const SelectFarmScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthStore>();

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: const Text('Choose a Farm'), automaticallyImplyLeading: false),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: auth.farms.length,
              itemBuilder: (context, i) {
                final farm = auth.farms[i];
                return Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: AgroCard(
                    child: ListTile(
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      title: Text(farm.name, style: Theme.of(context).textTheme.titleMedium),
                      subtitle: Text(
                        farm.setupStatus == FarmSetupStatus.configured ? 'Configured' : 'Draft',
                        style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone),
                      ),
                      trailing: const Icon(Icons.chevron_right, color: AgroColors.stone),
                      onTap: () => context.read<AuthStore>().selectFarm(farm),
                    ),
                  ),
                );
              },
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: TextButton(
              onPressed: () => context.read<AuthStore>().logout(),
              child: const Text('Log out'),
            ),
          ),
        ],
      ),
    );
  }
}
