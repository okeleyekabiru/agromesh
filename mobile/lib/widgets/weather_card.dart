import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/models.dart';
import '../theme/app_theme.dart';
import 'widgets.dart';

const _days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const _months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

String _short(DateTime d) => '${d.day} ${_months[d.month - 1]}';

/// Today's weather plus a scrollable 7-day forecast, with good spray days marked.
class WeatherCard extends StatelessWidget {
  final WeatherInfo weather;
  const WeatherCard({super.key, required this.weather});

  @override
  Widget build(BuildContext context) {
    final days = weather.forecast;
    if (days.isEmpty) return const SizedBox.shrink();
    final today = days.first;

    return AgroCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 10),
            child: Row(
              children: [
                Text(today.icon, style: const TextStyle(fontSize: 34)),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${today.tempMaxC.round()}°C',
                        style: GoogleFonts.syne(fontSize: 24, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                      Text(
                        [if (weather.location != null) weather.location!, if (weather.season.isNotEmpty) weather.season].join(' · '),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text('${today.precipitationMm.toStringAsFixed(1)} mm today',
                      style: GoogleFonts.jetBrainsMono(fontSize: 10, color: AgroColors.forest)),
                    const SizedBox(height: 2),
                    Text('${weather.rainfallLast30Days.round()} mm last 30 days',
                      style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
                  ],
                ),
              ],
            ),
          ),
          if (weather.nextDryWindowStart != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: AgroColors.leaf.withOpacity(0.07),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AgroColors.leaf.withOpacity(0.2)),
                ),
                child: Text(
                  '💧 Best spray window: ${_short(weather.nextDryWindowStart!)}'
                  '${weather.nextDryWindowEnd != null ? ' – ${_short(weather.nextDryWindowEnd!)}' : ''}',
                  style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w600, color: AgroColors.forest),
                ),
              ),
            ),
          const Divider(height: 1, color: AgroColors.fog),
          SizedBox(
            height: 142,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.all(12),
              itemCount: days.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (_, i) => _DayTile(day: days[i], isToday: i == 0),
            ),
          ),
        ],
      ),
    );
  }
}

class _DayTile extends StatelessWidget {
  final DailyForecast day;
  final bool isToday;
  const _DayTile({required this.day, required this.isToday});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 70,
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 4),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(10),
        color: isToday ? AgroColors.palm.withOpacity(0.07) : null,
        border: Border.all(color: isToday ? AgroColors.palm : AgroColors.fog),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(isToday ? 'Today' : _days[day.date.weekday - 1],
            style: GoogleFonts.syne(fontSize: 11, fontWeight: FontWeight.w700, color: AgroColors.forest)),
          Text(day.icon, style: const TextStyle(fontSize: 22)),
          Text('${day.tempMaxC.round()}° ${day.tempMinC.round()}°',
            style: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w600, color: AgroColors.forest)),
          Text('${day.precipitationMm.toStringAsFixed(1)}mm${day.rainChance > 0 ? ' ${day.rainChance}%' : ''}',
            style: GoogleFonts.jetBrainsMono(fontSize: 8, color: AgroColors.stone)),
          Text(day.goodSprayDay ? '💧 Spray' : '—',
            style: GoogleFonts.inter(fontSize: 10, fontWeight: FontWeight.w600,
              color: day.goodSprayDay ? AgroColors.leaf : AgroColors.fog)),
        ],
      ),
    );
  }
}
