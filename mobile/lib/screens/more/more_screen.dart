import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'activities_screen.dart';
import 'approvals_screen.dart';
import 'common.dart';
import 'farm_branding_screen.dart';
import 'finance_screen.dart';
import 'inventory_screen.dart';
import 'invoices_screen.dart';
import 'vendor_invoices_screen.dart';
import 'orders_screen.dart';
import 'reports_screen.dart';
import 'satellite_screen.dart';
import 'team_screen.dart';
import 'wage_bands_screen.dart';

/// The "More" tab: everything on the web app that is not one of the three main tabs.
class MoreScreen extends StatelessWidget {
  const MoreScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final manager = isManager(context);
    final items = <_Item>[
      _Item(Icons.list_alt_outlined, 'Activities', 'Every logged job, with details', () => const ActivitiesScreen()),
      _Item(Icons.inventory_2_outlined, 'Inventory', 'Stock levels, pick, adjust, set price', () => const InventoryScreen()),
      _Item(Icons.shopping_cart_outlined, 'Orders', 'Purchase orders: place, receive, pay', () => const OrdersScreen()),
      _Item(Icons.receipt_long_outlined, 'Invoices', 'Invoices you send to customers, as PDF', () => const InvoicesScreen()),
      _Item(Icons.request_quote_outlined, 'Vendor invoices', 'Bills from suppliers: paid, unpaid, overdue', () => const VendorInvoicesScreen()),
      _Item(Icons.satellite_alt_outlined, 'Satellite', 'Crop health: greenness and leaf water', () => const SatelliteScreen()),
      _Item(Icons.payments_outlined, 'Wage bands', 'Pay rates per worker role', () => const WageBandsScreen()),
      if (manager) _Item(Icons.account_balance_wallet_outlined, 'Finance', 'Bank transactions and review', () => const FinanceScreen()),
      if (manager) _Item(Icons.bar_chart_outlined, 'Reports', 'Monthly spend and cost breakdown', () => const ReportsScreen()),
      if (manager) _Item(Icons.fact_check_outlined, 'Approvals', 'Price changes and stock adjustments', () => const ApprovalsScreen()),
      if (manager) _Item(Icons.image_outlined, 'Farm logo & profile', 'Logo, address and bank details for invoices', () => const FarmBrandingScreen()),
      if (manager) _Item(Icons.group_outlined, 'Team', 'Managers and supervisors, access', () => const TeamScreen()),
    ];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        AgroCard(
          child: Column(
            children: [
              for (final it in items)
                InkWell(
                  onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => it.screen())),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                    decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: IlevoraColors.fog))),
                    child: Row(
                      children: [
                        Icon(it.icon, color: IlevoraColors.leaf, size: 22),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(it.title, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, fontWeight: FontWeight.w600, color: IlevoraColors.ink)),
                              const SizedBox(height: 2),
                              Text(it.sub, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: IlevoraColors.stone)),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right, color: IlevoraColors.stone, size: 18),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 80),
      ],
    );
  }
}

class _Item {
  final IconData icon;
  final String title;
  final String sub;
  final Widget Function() screen;
  const _Item(this.icon, this.title, this.sub, this.screen);
}
