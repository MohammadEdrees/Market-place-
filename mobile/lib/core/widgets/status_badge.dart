import 'package:flutter/material.dart';

/// Coloured pill for a listing's derived status:
/// `Active`, `Low stock` or `Out of stock`.
class StatusBadge extends StatelessWidget {
  const StatusBadge({super.key, required this.status});

  final String status;

  ({Color background, Color foreground}) get _palette => switch (status) {
        'Active' => (
            background: const Color(0xFFDCF5E4),
            foreground: const Color(0xFF1B7A43),
          ),
        'Low stock' => (
            background: const Color(0xFFFFF0D6),
            foreground: const Color(0xFF9A6400),
          ),
        'Out of stock' => (
            background: const Color(0xFFFDE3E3),
            foreground: const Color(0xFFB3261E),
          ),
        'Inactive' => (
            background: const Color(0xFFECECF3),
            foreground: const Color(0xFF5A5A6E),
          ),
        _ => (
            background: const Color(0xFFE3EDFF),
            foreground: const Color(0xFF2B5BC7),
          ),
      };

  @override
  Widget build(BuildContext context) {
    final palette = _palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: palette.background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        status,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: palette.foreground,
        ),
      ),
    );
  }
}
