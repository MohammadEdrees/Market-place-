import 'package:flutter/material.dart';

import 'listing_image.dart';

/// Swipeable image gallery with page dots; shows a neutral placeholder when
/// the listing has no photos yet.
class Gallery extends StatefulWidget {
  const Gallery({super.key, required this.images, this.height = 260});

  /// Absolute image URLs, already ordered by the API.
  final List<String> images;
  final double height;

  @override
  State<Gallery> createState() => _GalleryState();
}

class _GalleryState extends State<Gallery> {
  int _current = 0;

  @override
  Widget build(BuildContext context) {
    if (widget.images.isEmpty) {
      return Container(
        height: widget.height,
        color: ListingImage.placeholderColor,
        child: const Center(
          child: Icon(Icons.image_outlined, size: 64, color: Colors.grey),
        ),
      );
    }

    return SizedBox(
      height: widget.height,
      child: Stack(
        children: [
          PageView.builder(
            itemCount: widget.images.length,
            onPageChanged: (index) => setState(() => _current = index),
            itemBuilder: (context, index) => ListingImage(
              url: widget.images[index],
              fit: BoxFit.cover,
            ),
          ),
          if (widget.images.length > 1)
            Positioned(
              bottom: 10,
              left: 0,
              right: 0,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < widget.images.length; i++)
                    AnimatedContainer(
                      duration: const Duration(milliseconds: 180),
                      margin: const EdgeInsets.symmetric(horizontal: 3),
                      width: i == _current ? 18 : 8,
                      height: 8,
                      decoration: BoxDecoration(
                        color: i == _current
                            ? Colors.white
                            : Colors.white.withValues(alpha: 0.5),
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
