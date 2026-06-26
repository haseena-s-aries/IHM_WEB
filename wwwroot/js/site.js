// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    let formData = {}; // Store form data temporarily

    $(".btn-next").click(function () {
        let sectionData = {};
        $(this).closest(".form-section").find("input, select").each(function () {
            sectionData[$(this).attr("name")] = $(this).val();
        });

        Object.assign(formData, sectionData); // Merge data with existing formData

        console.log("Current Data:", formData); // Debugging

        // Show next section
        $(this).closest(".form-section").hide().next(".form-section").show();
    });

    $(".btn-submit").click(function () {
        $.ajax({
            url: "/api/Certificate/save-certificate",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(formData),
            success: function (response) {
                alert("Form saved successfully! Generating PDF...");
                window.location.href = `/api/Certificate/generate-pdf/${response.certificateId}`;
            },
            error: function (err) {
                alert("Error saving data!");
                console.log(err);
            }
        });
    });
});
