import 'package:flutter/material.dart';

/// Network image with a neutral placeholder while loading and a graceful
/// fallback when there is no image or the URL fails to load.
class ListingImage extends StatelessWidget {
  const ListingImage({super.key, required this.url, this.fit = BoxFit.cover});

  /// Absolute URL from the API, or `null`/empty for "no image".
  final String? url;
  final BoxFit fit;

  static const Color placeholderColor = Color(0xFFE9E9F1);

  @override
  Widget build(BuildContext context) {
    final location = url;
    if (location == null || location.isEmpty) return placeholder(null);

    return Image.network(
      location,
      fit: fit,
      loadingBuilder: (context, child, progress) {
        if (progress == null) return child;
        return placeholder(
          const SizedBox(
            width: 22,
            height: 22,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        );
      },
      errorBuilder: (context, error, stackTrace) => placeholder(null),
    );
  }

  Widget placeholder(Widget? child) => Container(
        color: placeholderColor,
        alignment: Alignment.center,
        child: child ??
            const Icon(Icons.image_outlined, size: 36, color: Colors.grey),
      );
}
