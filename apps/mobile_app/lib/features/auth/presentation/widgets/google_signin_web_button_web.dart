import 'package:flutter/widgets.dart';
import 'package:google_sign_in_web/web_only.dart' as gsi_web;

/// Trên Web, Google chỉ cấp idToken qua nút "Sign in with Google" render sẵn
/// của Google Identity Services — không qua lời gọi signIn() trực tiếp.
Widget renderGoogleWebButton() => gsi_web.renderButton();
