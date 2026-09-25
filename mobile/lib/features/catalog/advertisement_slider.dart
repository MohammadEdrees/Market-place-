import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/config/api_config.dart';
import 'catalog_providers.dart';
import 'models.dart';

/// Absolute URL for an ad image; relative paths (`/images/…`) are prefixed
/// with the configured API base. `null`/empty means "no image".
String? advertisementImageUrl(String? imagePath) {
  if (imagePath == null || imagePath.isEmpty) return null;
  if (imagePath.startsWith('/')) return '${ApiConfig.baseUrl}$imagePath';
  return imagePath;
}

/// Swipeable banner showing the active advertisement slides.
///
/// Renders nothing while the ads load, when the list is empty and when the
/// request fails, so the Products page never breaks because of ads.
class AdvertisementSlider extends ConsumerStatefulWidget {
  const AdvertisementSlider({
    super.key,
    this.autoAdvance = true,
    this.interval = const Duration(seconds: 5),
  });

  /// Flip to the next slide on a timer. Widget tests turn this off so no
  /// live `Timer.periodic` keeps `pumpAndSettle` waiting forever.
  final bool autoAdvance;

  /// Delay between automatic page flips.
  final Duration interval;

  @override
  ConsumerState<AdvertisementSlider> createState() =>
      _AdvertisementSliderState();
}

class _AdvertisementSliderState extends ConsumerState<AdvertisementSlider> {
  final PageController _pageController = PageController();
  Timer? _timer;

  int _current = 0;
  int _slideCount = 0;

  @override
  void dispose() {
    _timer?.cancel();
    _pageController.dispose();
    super.dispose();
  }

  /// Runs the auto-advance timer only while there is more than one slide;
  /// loading, errors and single-slide responses keep no timer alive.
  void _syncTimer(int slides) {
    final shouldRun = widget.autoAdvance && slides > 1;
    if (shouldRun) {
      _timer ??= Timer.periodic(widget.interval, (_) => _advance());
    } else if (_timer != null) {
      _timer!.cancel();
      _timer = null;
    }
  }

  void _advance() {
    if (!mounted || _slideCount < 2 || !_pageController.hasClients) return;
    _pageController.animateToPage(
      (_current + 1) % _slideCount,
      duration: const Duration(milliseconds: 350),
      curve: Curves.easeOut,
    );
  }

  /// `product/{id}` and `service/{id}` open their detail route; external
  /// links and anything unrecognized are deliberately a no-op.
  void _openTarget(String? target) {
    if (target == null || target.isEmpty) return;
    final product = RegExp(r'^product/(\d+)$').firstMatch(target);
    if (product != null) {
      context.go('/products/${product.group(1)}');
      return;
    }
    final service = RegExp(r'^service/(\d+)$').firstMatch(target);
    if (service != null) context.go('/services/${service.group(1)}');
  }

  @override
  Widget build(BuildContext context) {
    // Loading, errors and an empty list all resolve to no slides.
    final ads =
        ref.watch(activeAdvertisementsProvider).value ??
        const <Advertisement>[];
    _slideCount = ads.length;
    _syncTimer(ads.length);
    if (ads.isEmpty) return const SizedBox.shrink();

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(
          height: 150,
          child: PageView.builder(
            controller: _pageController,
            itemCount: ads.length,
            onPageChanged: (index) => setState(() => _current = index),
            itemBuilder: (context, index) => _Slide(
              ad: ads[index],
              onTap: () => _openTarget(ads[index].targetUrl),
            ),
          ),
        ),
        const SizedBox(height: 8),
        _PageIndicator(count: ads.length, current: _current),
      ],
    );
  }
}

class _Slide extends StatelessWidget {
  const _Slide({required this.ad, this.onTap});

  final Advertisement ad;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final imageUrl = advertisementImageUrl(ad.imagePath);
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(14),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onTap,
            child: Stack(
              fit: StackFit.expand,
              children: [
                // Theme gradient: visible while a network image loads and
                // the fallback card when there is no image or it fails.
                DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        theme.colorScheme.primary,
                        theme.colorScheme.tertiary,
                      ],
                    ),
                  ),
                ),
                if (imageUrl != null)
                  Image.network(
                    imageUrl,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) =>
                        const SizedBox.shrink(),
                  ),
                // Subtle scrim so the copy stays readable over any photo.
                const DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [Colors.transparent, Colors.black54],
                      stops: [0.35, 1],
                    ),
                  ),
                ),
                Positioned(
                  left: 14,
                  right: 14,
                  bottom: 12,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        ad.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.titleMedium?.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      if (ad.subtitle.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          ad.subtitle,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: Colors.white70,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Dot row under the cards showing which slide is on screen.
class _PageIndicator extends StatelessWidget {
  const _PageIndicator({required this.count, required this.current});

  final int count;
  final int current;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        for (var i = 0; i < count; i++)
          AnimatedContainer(
            duration: const Duration(milliseconds: 200),
            margin: const EdgeInsets.symmetric(horizontal: 3),
            width: i == current ? 16 : 6,
            height: 6,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(3),
              color: i == current
                  ? theme.colorScheme.primary
                  : theme.colorScheme.surfaceContainerHighest,
            ),
          ),
      ],
    );
  }
}
