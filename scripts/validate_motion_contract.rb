#!/usr/bin/env ruby
# Validates the core BM8 penalty motion rules that are easy to regress.

root = File.expand_path("..", __dir__)
source_path = File.join(root, "Assets", "Scripts", "Bm8PenaltyPrototype.cs")

unless File.exist?(source_path)
  warn "Missing source file: #{source_path}"
  exit 1
end

source = File.read(source_path)

def section(source, start_pattern, end_pattern)
  source[/#{start_pattern}.*?#{end_pattern}/m] || ""
end

fly_ball = section(source, "private IEnumerator FlyBall", "private void CacheBodyParts")
aa_deflect = section(source, "private Vector3 AaDeflectWorld", "private bool AaUsesCatchSave")
aa_catch = section(source, "private Vector3 AaCatchSettleWorld", "private float AaShotArcHeight")
low_side_settle = section(source, "private void ApplyLowSideSaveSettleMotion", "private void ApplyKeeperLowSideFootIk")

checks = {
  "old box aiming guide removed" => !source.include?("DrawReadyAimGuide") && !source.include?("DrawAimingBallMarker"),
  "ball-style target marker active" => source.include?("DrawAimingTargetMarker"),
  "yellow shot effects disabled" => source.include?("private static readonly bool ShowShotYellowEffects = false;"),
  "result camera punch disabled" => source.match?(/private IEnumerator ResultCameraPunch\([^\)]*\)\s*\{\s*yield break;\s*\}/m),
  "saved ball path has no random deflection" => !fly_ball.include?("Random.Range"),
  "AA deflect path has no random deflection" => !aa_deflect.include?("Random.Range"),
  "AA catch path has no random settle/drop" => !aa_catch.include?("Random.Range"),
  "saved ball load has no lateral sine drift" => !fly_ball.include?("position.x += Mathf.Sin"),
  "saved ball uses smoother load" => fly_ball.include?("Vector3.Lerp(contact, palmLoad, Smoother(loadT))"),
  "miss result waits for full banner" => source.include?(": Mathf.Max(UseAaAnimatedKeeper ? aaProfile.resultHold : 1.4f, resultBannerUntil - Time.time + 0.28f);"),
  "catch saves clamp ball to active hand" => source.include?("ball.position = ClampAaCatchBallToHand(ball.position, t, contactTime);"),
  "low side saves ground the whole body" => source.include?("private void LowerLowSideVisibleModelToGround(float weight)"),
  "low side saves use down clips instead of hold-ball clips" => source.include?("return \"AA_Soccer_Goal_Down_LD\";") &&
    source.include?("return \"AA_Soccer_Goal_Down_RD\";") &&
    !source.include?("AA_Soccer_Goal_HoldBall_LD") &&
    !source.include?("AA_Soccer_Goal_HoldBall_RD"),
  "low side saves play the dive down to the ground" => source.include?("poseHoldT = center ? 0.62f : 0.85f,"),
  "low side saves keep natural lateral drive" => source.include?("rootSide = center ? 0f : 1.06f,"),
  "low side settle is time-based, not frame-accumulating" => !low_side_settle.include?("keeperVisibleModel.position +=") &&
    !low_side_settle.include?("keeperVisibleModel.rotation =") &&
    source.include?("float lowSideSettle = bottom && side != 0f && keeperCurrentSave"),
  "player build loads the same stylized keeper as editor" => source.include?("EnsureUploadedStylizedKeeper();") &&
    source.include?("Resources.Load<GameObject>(UploadedStylizedKeeperResource)") &&
    source.include?("Resources.Load<Texture2D>(Bm8KeeperBaseTextureResource)")
}

failed = checks.select { |_name, passed| !passed }
checks.each do |name, passed|
  puts "#{passed ? "OK" : "FAIL"} #{name}"
end

exit(failed.empty? ? 0 : 1)
