$(document).ready(function () {
  $("#certificateForm").on("submit", function (e) {
    e.preventDefault();
    const formdata = new FormData(this);
    showSpinner();
    fetch("/Officer/UploadDSC", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        hideSpinner();
        if (data.status) {
          $("#certificateForm").after(
            `<p class="text-success fs-4">${data.message}</p>`
          );
        } else {
          $("#certificateForm").after(
            `<p class="errorMsg f-4">${data.message}</p>`
          );
        }
      });
    console.log(formdata);
  });
});
