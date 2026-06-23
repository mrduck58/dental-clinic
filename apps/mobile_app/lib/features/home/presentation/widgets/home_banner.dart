import 'package:flutter/material.dart';

class HomeBanner extends StatelessWidget {
  const HomeBanner({super.key});

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(22),
      child: Image.asset(
        'assets/images/banner_1.png',
        width: double.infinity,
        height: 200,
        fit: BoxFit.cover,
      ),
    );
  }
}
