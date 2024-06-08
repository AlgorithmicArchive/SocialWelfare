function GenerateBackupCodes() {
  fetch("/Profile/GenerateBackupCodes", { method: "get" })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = data.url;
      }
    });
}

$(document).ready(function () {
  const { backupCodes: rawBackupCodes } = userDetails;
  const backupCodes = JSON.parse(rawBackupCodes);
  $("#backupCodes")
    .parent()
    .append(
      `<button class="btn btn-dark text-center" onclick='GenerateBackupCodes();'>Regenerate Backup Codes</button>`
    );

  backupCodes.unused.forEach((item) => {
    $("#backupCodes").append(
      `<div class="col-md-3 border border-dark bg-success text-white fw-bold  rounded-pill p-2 text-center">${item}</div>`
    );
  });
  backupCodes.used.forEach((item) => {
    $("#backupCodes").append(
      `<div class="col-md-3 border border-dark bg-warning fw-bold rounded-pill p-2 text-center">${item}</div>`
    );
  });
});
