output "pubsub_topic_name" {
  description = "Name of the created Pub/Sub topic"
  value       = google_pubsub_topic.image_processing_topic.name
}

output "pubsub_subscription_name" {
  description = "Name of the created Pub/Sub subscription"
  value       = google_pubsub_subscription.image_processing_subscription.name
}

output "dead_letter_topic_name" {
  description = "Name of the dead letter topic"
  value       = google_pubsub_topic.image_processing_dead_letter.name
}

output "service_account_email" {
  description = "Email of the service account"
  value       = google_service_account.image_processing_sa.email
}

output "service_account_key" {
  description = "Base64 encoded service account key"
  value       = google_service_account_key.image_processing_key.private_key
  sensitive   = true
}

output "storage_bucket_name" {
  description = "Name of the storage bucket for processed images"
  value       = google_storage_bucket.processed_images.name
}

output "storage_bucket_url" {
  description = "URL of the storage bucket"
  value       = google_storage_bucket.processed_images.url
}

output "project_id" {
  description = "The Google Cloud project ID"
  value       = var.project_id
}

output "region" {
  description = "The Google Cloud region"
  value       = var.region
}
