import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:market_workplace/core/config/api_config.dart';
import 'package:market_workplace/features/catalog/advertisement_slider.dart';
import 'package:market_workplace/features/catalog/catalog_providers.dart';
import 'package:market_workplace/features/catalog/models.dart';

void main() {
  /// Minimal router mirroring the app's detail routes so tap-to-navigate can
  /// be asserted the same way `login_page_test` does.
  GoRouter buildRouter({
    bool autoAdvance = false,
    Duration interval = const Duration(seconds: 5),
  }) => GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => Scaffold(
          body: AdvertisementSlider(
            autoAdvance: autoAdvance,
            interval: interval,
          ),
        ),
      ),
      GoRoute(
        path: '/products/:id',
        builder: (context, state) =>
            Scaffold(body: Text('product ${state.pathParameters['id']}')),
      ),
      GoRoute(
        path: '/services/:id',
        builder: (context, state) =>
            Scaffold(body: Text('service ${state.pathParameters['id']}')),
      ),
    ],
  );

  Future<void> pumpSlider(
    WidgetTester tester, {
    required AsyncValue<List<Advertisement>> ads,
    bool autoAdvance = false,
    Duration interval = const Duration(seconds: 5),
    bool settle = true,
  }) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [activeAdvertisementsProvider.overrideWithValue(ads)],
        child: MaterialApp.router(
          routerConfig: buildRouter(
            autoAdvance: autoAdvance,
            interval: interval,
          ),
        ),
      ),
    );
    // Never settle while an auto-advance timer is alive.
    if (settle) await tester.pumpAndSettle();
  }

  /// Current dot widths; the wide one marks the active slide.
  List<double?> dotWidths(WidgetTester tester) => tester
      .widgetList<AnimatedContainer>(find.byType(AnimatedContainer))
      .map((dot) => dot.constraints?.maxWidth)
      .toList();

  testWidgets('renders a slide with its title and subtitle', (tester) async {
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 1,
          title: 'Weekend Audio Sale',
          subtitle: 'Up to 30% off headsets, earbuds and speakers',
          targetUrl: 'product/1',
          imagePath: 'http://localhost:5240/images/ads/ad-1.png',
        ),
      ]),
    );

    expect(find.byType(PageView), findsOneWidget);
    expect(find.text('Weekend Audio Sale'), findsOneWidget);
    expect(
      find.text('Up to 30% off headsets, earbuds and speakers'),
      findsOneWidget,
    );
    expect(find.byType(AnimatedContainer), findsOneWidget);
  });

  testWidgets('renders nothing when there are no ads', (tester) async {
    await pumpSlider(tester, ads: AsyncValue.data(const []));

    expect(find.byType(PageView), findsNothing);
    expect(find.byType(AnimatedContainer), findsNothing);
    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is SizedBox && widget.width == 0 && widget.height == 0,
      ),
      findsOneWidget,
    );
    // The widget itself stays mounted — it just hides its content.
    expect(find.byType(AdvertisementSlider), findsOneWidget);
  });

  testWidgets('stays hidden while loading and when the request fails', (
    tester,
  ) async {
    await pumpSlider(tester, ads: AsyncValue.loading());
    expect(find.byType(PageView), findsNothing);

    await pumpSlider(
      tester,
      ads: AsyncValue.error(Exception('boom'), StackTrace.current),
    );
    expect(find.byType(PageView), findsNothing);
    expect(find.byType(AdvertisementSlider), findsOneWidget);
  });

  testWidgets('falls back to a gradient card when imagePath is null', (
    tester,
  ) async {
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 1,
          title: 'Weekend Audio Sale',
          subtitle: 'Up to 30% off',
        ),
      ]),
    );

    expect(find.byType(Image), findsNothing);
    expect(find.text('Weekend Audio Sale'), findsOneWidget);
    expect(find.text('Up to 30% off'), findsOneWidget);
    // Base gradient and scrim keep the copy readable without a photo.
    expect(
      find.byWidgetPredicate(
        (widget) =>
            widget is DecoratedBox && widget.decoration is BoxDecoration,
      ),
      findsWidgets,
    );
  });

  testWidgets('falls back to the gradient card when the image fails to load', (
    tester,
  ) async {
    // The mocked HTTP layer rejects every request, so the network image
    // always fails here.
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 1,
          title: 'Weekend Audio Sale',
          imagePath: 'http://localhost:5240/images/ads/ad-1.png',
        ),
      ]),
    );

    expect(find.text('Weekend Audio Sale'), findsOneWidget);
  });

  testWidgets('tapping a product slide opens the product detail route', (
    tester,
  ) async {
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 1,
          title: 'Weekend Audio Sale',
          subtitle: 'Up to 30% off',
          targetUrl: 'product/1',
        ),
      ]),
    );

    await tester.tap(find.text('Weekend Audio Sale'));
    await tester.pumpAndSettle();

    expect(find.text('product 1'), findsOneWidget);
  });

  testWidgets('tapping a service slide opens the service detail route', (
    tester,
  ) async {
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 7,
          title: 'Book a Technician',
          targetUrl: 'service/7',
        ),
      ]),
    );

    await tester.tap(find.text('Book a Technician'));
    await tester.pumpAndSettle();

    expect(find.text('service 7'), findsOneWidget);
  });

  testWidgets('external https targets are a no-op', (tester) async {
    await pumpSlider(
      tester,
      ads: AsyncValue.data([
        const Advertisement(
          id: 1,
          title: 'Mega Deals',
          targetUrl: 'https://example.com/deals',
        ),
      ]),
    );

    await tester.tap(find.text('Mega Deals'));
    await tester.pumpAndSettle();

    expect(find.byType(AdvertisementSlider), findsOneWidget);
    expect(find.text('product 1'), findsNothing);
  });

  testWidgets('auto-advances to the next slide after the interval', (
    tester,
  ) async {
    // No pumpAndSettle here: the live periodic timer would never let it end.
    await pumpSlider(
      tester,
      ads: AsyncValue.data(const [
        Advertisement(id: 1, title: 'Slide one'),
        Advertisement(id: 2, title: 'Slide two'),
      ]),
      autoAdvance: true,
      interval: const Duration(seconds: 1),
      settle: false,
    );

    expect(dotWidths(tester), [16, 6]);

    await tester.pump(const Duration(seconds: 1)); // timer fires
    await tester.pump(const Duration(milliseconds: 400)); // page animation
    await tester.pump(); // rebuild the indicator

    expect(dotWidths(tester), [6, 16]);
    expect(find.text('Slide two'), findsOneWidget);
  });

  test('relative image paths are prefixed with the API base', () {
    expect(advertisementImageUrl(null), isNull);
    expect(advertisementImageUrl(''), isNull);
    expect(
      advertisementImageUrl('/images/ads/ad-1.png'),
      '${ApiConfig.baseUrl}/images/ads/ad-1.png',
    );
    expect(
      advertisementImageUrl('http://localhost:5240/images/ads/ad-2.png'),
      'http://localhost:5240/images/ads/ad-2.png',
    );
  });
}
